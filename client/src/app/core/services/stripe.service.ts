import { inject, Injectable } from '@angular/core';
import {
  loadStripe,
  Stripe,
  StripeAddressElement,
  StripeAddressElementOptions,
  StripeElements,
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
  private stripePromise: Promise<Stripe | null> | null = null;
  private elements?: StripeElements;
  private addressElement?: StripeAddressElement;

  private getPublicKey(): string {
    if (!this.publicKey) {
      throw new Error('Stripe public key missing. Set STRIPE_PUBLIC_KEY in assets/env.js');
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

  async createAddressElement() {
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
              line1: address.street,
              city: address.city,
              state: address.state,
              postal_code: address.postalCode,
              country: 'IT',
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
}
