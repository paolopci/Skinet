import { inject, Injectable } from '@angular/core';
import {
  loadStripe,
  PaymentIntent,
  Stripe,
  StripeAddressElement,
  StripeAddressElementOptions,
  StripeElements,
  StripeError,
  StripePaymentElement,
  StripePaymentElementOptions,
} from '@stripe/stripe-js';
import { environment } from '../../../environments/environment.development';
import { CartService } from './cart.service';
import { HttpClient } from '@angular/common/http';
import { Cart } from '../../shared/models/cart';
import { firstValueFrom, map } from 'rxjs';
import { AccountService } from './account.service';
import { AuthStateService } from './auth-state.service';

@Injectable({
  providedIn: 'root',
})
export class StripeService {
  baseUrl = environment.apiUrl;
  private cartService = inject(CartService);
  private accountService = inject(AccountService);
  private authState = inject(AuthStateService);
  private http = inject(HttpClient);

  private readonly publicKey = environment.stripePublicKey?.trim();
  private readonly stripeMode = environment.stripeMode;
  private stripePromise: Promise<Stripe | null> | null = null;
  private elements?: StripeElements;
  private addressElement?: StripeAddressElement;
  private paymentElement?: StripePaymentElement;
  private lastUserEmail?: string;

  private static readonly paymentErrorFallbackMessage =
    'Pagamento non riuscito. Verifica i dati e riprova.';

  private getPublicKey(): string {
    if (!this.publicKey) {
      throw new Error('Stripe public key missing. Set STRIPE_PUBLIC_KEY in assets/env.js');
    }

    const isTestKey = this.publicKey.startsWith('pk_test_');
    const isLiveKey = this.publicKey.startsWith('pk_live_');

    if (!isTestKey && !isLiveKey) {
      throw new Error('Invalid Stripe public key format. Use pk_test_* or pk_live_* in assets/env.js');
    }

    if (this.stripeMode !== 'test' && this.stripeMode !== 'live') {
      throw new Error('Invalid STRIPE_MODE. Use "test" or "live" in assets/env.js');
    }

    if (this.stripeMode === 'test' && isLiveKey) {
      throw new Error('Stripe key/mode mismatch: STRIPE_MODE is test but public key is live');
    }

    if (this.stripeMode === 'live' && isTestKey) {
      throw new Error('Stripe key/mode mismatch: STRIPE_MODE is live but public key is test');
    }

    return this.publicKey;
  }

  private ensureStripeLoaded(): void {
    if (!this.stripePromise) {
      this.stripePromise = loadStripe(this.getPublicKey());
    }
  }

  getStripe(): Promise<Stripe | null> {
    this.ensureStripeLoaded();
    return this.stripePromise!;
  }

  async initializeElements() {
    if (!this.elements) {
      this.ensureUserContext();
      const stripe = await this.getStripe();
      if (stripe) {
        const cart = await firstValueFrom(this.createOrUpdatePaymentIntent());
        this.elements = stripe.elements({
          clientSecret: cart.clientsecret,
          appearance: { labels: 'floating' },
        });
      } else {
        throw new Error('Stripe has not been loaded');
      }
    }
    return this.elements;
  }

  private ensureUserContext() {
    const currentEmail = this.authState.user()?.email ?? null;
    if (this.lastUserEmail !== currentEmail) {
      this.resetStripeSession();
      this.lastUserEmail = currentEmail ?? undefined;
    }
  }

  private resetStripeSession() {
    this.destroyAddressElement();
    this.destroyPaymentElement();
    this.elements = undefined;
    this.stripePromise = null;
  }

  async createAddressElement(forceReload = false) {
    this.ensureUserContext();
    if (forceReload) {
      this.destroyAddressElement();
    }

    if (!this.addressElement) {
      const elements = await this.initializeElements();
      if (elements) {
        const options: StripeAddressElementOptions = {
          mode: 'shipping',
        };
        const user = this.authState.user();
        if (user?.firstName || user?.lastName) {
          options.defaultValues = {
            ...(options.defaultValues ?? {}),
            firstName: user?.firstName ?? null,
            lastName: user?.lastName ?? null,
          };
          options.display = {
            ...(options.display ?? {}),
            name: 'split',
          };
        }
        try {
          const address = await firstValueFrom(this.accountService.getAddress());
          options.defaultValues = {
            ...(options.defaultValues ?? {}),
            address: {
              line1: address.addressLine1,
              line2: address.addressLine2 ?? undefined,
              city: address.city,
              state: address.region ?? undefined,
              postal_code: address.postalCode,
              country: address.countryCode,
            },
          };
        } catch {
          // Nessun indirizzo salvato, manteniamo il form vuoto.
        }
        this.addressElement = elements.create('address', options);
      } else {
        throw new Error('Elements instance has not been loaded');
      }
    }
    return this.addressElement;
  }

  private destroyAddressElement() {
    if (!this.addressElement) {
      return;
    }

    try {
      this.addressElement.destroy();
    } catch {
      // Ignoriamo errori di distruzione e procediamo con una nuova istanza.
    } finally {
      this.addressElement = undefined;
    }
  }

  async createPaymentElement(forceReload = false) {
    this.ensureUserContext();
    if (forceReload) {
      this.destroyPaymentElement();
    }

    if (!this.paymentElement) {
      const elements = await this.initializeElements();
      if (elements) {
        const options: StripePaymentElementOptions = {
          layout: 'tabs',
        };
        this.paymentElement = elements.create('payment', options);
      } else {
        throw new Error('Elements instance has not been loaded');
      }
    }

    return this.paymentElement;
  }

  private destroyPaymentElement() {
    if (!this.paymentElement) {
      return;
    }

    try {
      this.paymentElement.destroy();
    } catch {
      // Ignoriamo errori di distruzione e procediamo con una nuova istanza.
    } finally {
      this.paymentElement = undefined;
    }
  }

  createOrUpdatePaymentIntent() {
    const cart = this.cartService.cart();
    if (!cart) {
      throw new Error('Problem with cart');
    }

    return this.http.post<Cart>(this.baseUrl + 'payments/' + cart.id, {}).pipe(
      map((cart) => {
        this.cartService.cart.set(cart);
        return cart;
      }),
    );
  }

  async confirmPayment(): Promise<PaymentConfirmationResult> {
    const stripe = await this.getStripe();
    if (!stripe) {
      return {
        isSuccess: false,
        status: 'stripe_not_loaded',
        message: 'Stripe non è stato caricato correttamente.',
      };
    }

    if (!this.elements) {
      return {
        isSuccess: false,
        status: 'missing_elements',
        message: 'Elementi di pagamento non inizializzati.',
      };
    }

    const submitResult = await this.elements.submit();
    if (submitResult.error) {
      return {
        isSuccess: false,
        status: submitResult.error.code ?? 'submit_error',
        message: this.mapStripeError(submitResult.error),
      };
    }

    const result = await stripe.confirmPayment({
      elements: this.elements,
      redirect: 'if_required',
    });

    if (result.error) {
      return {
        isSuccess: false,
        status: result.error.code ?? 'confirm_error',
        message: this.mapStripeError(result.error),
      };
    }

    const paymentIntent = result.paymentIntent;
    if (!paymentIntent) {
      return {
        isSuccess: false,
        status: 'missing_payment_intent',
        message: 'Stripe non ha restituito un PaymentIntent valido.',
      };
    }

    if (paymentIntent.status !== 'succeeded') {
      return {
        isSuccess: false,
        status: paymentIntent.status,
        paymentIntentId: paymentIntent.id,
        message: this.mapPaymentIntentStatus(paymentIntent),
      };
    }

    return {
      isSuccess: true,
      status: paymentIntent.status,
      paymentIntentId: paymentIntent.id,
    };
  }

  finalizePayment(cartId: string, paymentIntentId?: string) {
    return this.http.post<FinalizePaymentResponse>(`${this.baseUrl}payments/${cartId}/finalize`, {
      paymentIntentId,
    });
  }

  private mapStripeError(error: StripeError): string {
    const code = error.code ?? '';
    if (code === 'card_declined') {
      return 'Carta rifiutata. Usa un metodo di pagamento diverso.';
    }

    if (code === 'authentication_required') {
      return 'Autenticazione richiesta. Completa la verifica 3D Secure.';
    }

    if (code === 'expired_card') {
      return 'Carta scaduta. Aggiorna i dati della carta.';
    }

    if (code === 'incorrect_cvc') {
      return 'CVC non valido. Controlla il codice di sicurezza.';
    }

    if (code === 'processing_error') {
      return 'Errore temporaneo del provider di pagamento. Riprova tra poco.';
    }

    return error.message ?? StripeService.paymentErrorFallbackMessage;
  }

  private mapPaymentIntentStatus(paymentIntent: PaymentIntent): string {
    if (paymentIntent.status === 'requires_action') {
      return 'Autenticazione aggiuntiva richiesta. Completa la verifica e riprova.';
    }

    if (paymentIntent.status === 'requires_payment_method') {
      return 'Metodo di pagamento non valido o rifiutato.';
    }

    if (paymentIntent.status === 'processing') {
      return 'Pagamento in elaborazione. Attendi qualche secondo e riprova.';
    }

    return `Pagamento non completato. Stato corrente: ${paymentIntent.status}.`;
  }
}

export type PaymentConfirmationResult = {
  isSuccess: boolean;
  paymentIntentId?: string;
  status?: string;
  message?: string;
};

export type FinalizePaymentResponse = {
  isSuccess: boolean;
  status: string;
  orderId?: number;
  paymentIntentId?: string;
  message?: string;
};
