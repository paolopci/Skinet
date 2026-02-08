import {
  ChangeDetectorRef,
  Component,
  computed,
  inject,
  NgZone,
  OnDestroy,
  OnInit,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { OrderSummaryComponent } from '../../shared/components/order-summary/order-summary.component';
import { MATERIAL_IMPORTS } from '../../shared/material';
import { CartService } from '../../core/services/cart.service';
import { StripeService } from '../../core/services/stripe.service';
import { Address, StripeAddressElement, StripePaymentElement } from '@stripe/stripe-js';
import { SnackbarService } from '../../core/services/snackbar.service';
import { MatStepper } from '@angular/material/stepper';
import { MatCheckboxChange } from '@angular/material/checkbox';
import { StepperSelectionEvent } from '@angular/cdk/stepper';
import { AccountService } from '../../core/services/account.service';
import { AuthStateService } from '../../core/services/auth-state.service';
import { firstValueFrom } from 'rxjs';
import { AddressRequest } from '../../shared/models/auth';
import { extractValidationErrorMap } from '../../shared/utils/api-error';
import { FormsModule } from '@angular/forms';
import { CheckoutDeliveryComponent } from './checkout-delivery/checkout-delivery.component';
import { CheckoutService } from '../../core/services/checkout.service';
import { PaymentMethodsService } from '../../core/services/payment-methods.service';
import { SavedPaymentMethod } from '../../shared/models/saved-payment-method';
import { OrdersService } from '../../core/services/orders.service';

@Component({
  selector: 'app-checkout',
  imports: [
    OrderSummaryComponent,
    RouterLink,
    FormsModule,
    ...MATERIAL_IMPORTS,
    CheckoutDeliveryComponent,
  ],
  standalone: true,
  templateUrl: './checkout.component.html',
  styleUrl: './checkout.component.scss',
})
export class CheckoutComponent implements OnInit, OnDestroy {
  private static readonly supportedCountryCodes = new Set(['IT', 'US', 'GB']);
  private static readonly regionRequiredCountryCodes = new Set(['IT', 'US']);
  private static readonly strictRegionCountryCodes = new Set(['IT', 'US']);
  private stripeService = inject(StripeService);
  private snackbar = inject(SnackbarService);
  private accountService = inject(AccountService);
  private authState = inject(AuthStateService);
  private ngZone = inject(NgZone);
  private cdr = inject(ChangeDetectorRef);
  private paymentMethodsService = inject(PaymentMethodsService);
  private ordersService = inject(OrdersService);
  cartService = inject(CartService);
  checkoutService = inject(CheckoutService);
  addressElement?: StripeAddressElement;
  paymentElement?: StripePaymentElement;
  saveAddress = false;
  isProceedingToShipping = false;
  isReturningToAddress = false;
  pendingAddressReload = false;
  isLoadingPaymentElement = false;
  isProcessingPayment = false;
  isPaymentElementReady = false;
  isPaymentElementComplete = false;
  paymentElementError: string | null = null;
  isOrderConfirmed = false;
  confirmedOrderId: number | null = null;
  confirmationMessage: string | null = null;
  savePaymentMethod = false;
  savedPaymentMethods: SavedPaymentMethod[] = [];
  selectedSavedPaymentMethodId: string | null = null;
  isLoadingSavedPaymentMethods = false;
  private initialValueSnapshot: string | null = null;
  private lastPolledValue: string | null = null;
  private addressPollingTimerId: ReturnType<typeof setInterval> | null = null;

  subtotal = computed(
    () =>
      this.cartService
        .cart()
        ?.items.reduce((total, item) => total + item.price * item.quantity, 0) ?? 0,
  );
  shippingCost = computed(() => this.checkoutService.selectedDeliveryMethod()?.price ?? 0);
  discount = computed(() => 0);
  orderTotal = computed(() => this.subtotal() + this.shippingCost() - this.discount());

  async ngOnInit() {
    try {
      await this.loadDefaultAddressSnapshotFromApi();
      await this.loadSavedPaymentMethods();
      this.addressElement = await this.stripeService.createAddressElement();
      await this.mountAddressElement(this.addressElement);
      this.bindAddressChangeHandler();
      // Ritarda l'avvio del polling per dare tempo a Stripe di popolare il form
      setTimeout(() => this.startAddressPolling(), 1000);
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Errore inatteso.';
      this.snackbar.showError(message);
    }

    // this.startAddressSyncMonitor();
  }

  ngOnDestroy(): void {
    this.stopAddressPolling();
  }

  async goToShipping(stepper: MatStepper) {
    if (this.isProceedingToShipping) {
      return;
    }

    this.isProceedingToShipping = true;
    try {
      if (!this.addressElement) {
        this.snackbar.showError('Indirizzo non disponibile.');
        return;
      }

      const { complete } = await this.addressElement.getValue();
      if (!complete) {
        this.snackbar.showWarning(
          'Completa tutti i campi obbligatori dell’indirizzo prima di proseguire.',
        );
        return;
      }

      const hasValidShippingAddress = await this.validateShippingAddressForCheckout();
      if (!hasValidShippingAddress) {
        return;
      }

      if (this.saveAddress) {
        const isAddressSaved = await this.saveAddressAsDefault();
        if (!isAddressSaved) {
          return;
        }

        this.snackbar.showInfo('Indirizzo predefinito aggiornato.');
      }

      stepper.next();
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Errore inatteso.';
      this.snackbar.showError(message);
    } finally {
      this.isProceedingToShipping = false;
    }
  }

  onSaveAddressCheckboxChange(event: MatCheckboxChange) {
    if (this.isProceedingToShipping) {
      return;
    }

    this.saveAddress = event.checked;
  }

  async goBackToAddress(stepper: MatStepper) {
    if (this.isReturningToAddress) {
      return;
    }

    this.isReturningToAddress = true;
    this.pendingAddressReload = true;
    stepper.previous();
  }

  async onStepChange(event: StepperSelectionEvent) {
    if (event.selectedIndex === 0 && this.pendingAddressReload) {
      this.pendingAddressReload = false;
      try {
        await this.remountAddressElementFromAccount();
        this.snackbar.showInfo('Indirizzo ricaricato dal profilo.');
      } catch (error) {
        const message = error instanceof Error ? error.message : 'Errore inatteso.';
        this.snackbar.showWarning(`Ritorno su Address senza ricarica profilo: ${message}`);
      } finally {
        this.isReturningToAddress = false;
      }
    }

    if (event.selectedIndex === 2) {
      await this.initializePaymentElementForStep();
    }
  }

  onSavePaymentMethodCheckboxChange(event: MatCheckboxChange): void {
    this.savePaymentMethod = event.checked;
  }

  onSelectSavedPaymentMethod(paymentMethodId: string): void {
    this.selectedSavedPaymentMethodId = paymentMethodId;
    this.savePaymentMethod = false;
    this.isPaymentElementReady = true;
    this.isPaymentElementComplete = true;
    this.paymentElementError = null;
    this.paymentElement?.destroy();
    this.paymentElement = undefined;
  }

  onUseNewCard(): void {
    if (!this.selectedSavedPaymentMethodId) {
      return;
    }

    this.selectedSavedPaymentMethodId = null;
    void this.initializePaymentElementForStep();
  }

  private async loadSavedPaymentMethods(): Promise<void> {
    this.isLoadingSavedPaymentMethods = true;
    try {
      const methods = await firstValueFrom(this.paymentMethodsService.getSavedPaymentMethods());
      this.savedPaymentMethods = methods;
      const defaultMethod = methods.find((method) => method.isDefault) ?? null;
      this.selectedSavedPaymentMethodId = defaultMethod?.id ?? null;
    } catch (error) {
      this.savedPaymentMethods = [];
      this.selectedSavedPaymentMethodId = null;
      if (error instanceof HttpErrorResponse) {
        if (error.status === 401) {
          this.snackbar.showWarning(
            'Sessione scaduta. Effettua di nuovo l’accesso per vedere le carte salvate.',
          );
        } else if (error.status === 403) {
          this.snackbar.showWarning('Non sei autorizzato a visualizzare i metodi salvati.');
        } else if (error.status === 502) {
          this.snackbar.showWarning(
            'Provider pagamenti non disponibile. Mostro solo inserimento nuova carta.',
          );
        }
      }
    } finally {
      this.isLoadingSavedPaymentMethods = false;
    }
  }

  private async saveAddressAsDefault(): Promise<boolean> {
    const payload = await this.getAddressPayloadFromStripe();
    if (!payload) {
      this.snackbar.showError('Impossibile salvare l’indirizzo predefinito.');
      return false;
    }

    try {
      await firstValueFrom(this.accountService.updateAddress(payload));
      const result = await this.addressElement?.getValue();
      if (result) {
        this.initialValueSnapshot = JSON.stringify(result.value);
      }
      this.saveAddress = true;
      return true;
    } catch (error) {
      this.showAddressSaveError(error);
      return false;
    }
  }

  private async getAddressPayloadFromStripe(showWarnings = true): Promise<AddressRequest | null> {
    if (!this.addressElement) {
      return null;
    }

    const user = this.authState.user();
    if (!user?.firstName || !user?.lastName) {
      return null;
    }

    const result = await this.addressElement.getValue();
    const address = result.value.address;
    if (!address) {
      return null;
    }

    const firstName = this.normalizeRequired(user.firstName);
    const lastName = this.normalizeRequired(user.lastName);
    const addressLine1 = this.normalizeRequired(address.line1);
    const city = this.normalizeRequired(address.city);
    const postalCode = this.normalizeRequired(address.postal_code);
    const countryCode = this.normalizeRequired(address.country).toUpperCase();
    const region = this.normalizeOptional(address.state);
    const addressLine2 = this.normalizeOptional(address.line2);

    if (
      !firstName ||
      !lastName ||
      !addressLine1 ||
      !city ||
      !postalCode ||
      countryCode.length !== 2
    ) {
      return null;
    }

    if (!CheckoutComponent.supportedCountryCodes.has(countryCode)) {
      if (showWarnings) {
        this.snackbar.showWarning('Paese non supportato. Usa IT, US o GB.');
      }
      return null;
    }

    const isRegionRequired = CheckoutComponent.regionRequiredCountryCodes.has(countryCode);
    if (isRegionRequired && !region) {
      if (showWarnings) {
        this.snackbar.showWarning('Provincia/Stato obbligatorio per il paese selezionato.');
      }
      return null;
    }

    return {
      firstName,
      lastName,
      addressLine1,
      addressLine2,
      city,
      postalCode,
      countryCode,
      region,
    };
  }

  private async validateShippingAddressForCheckout(): Promise<boolean> {
    if (!this.addressElement) {
      return false;
    }

    const result = await this.addressElement.getValue();
    const address = result.value.address;
    if (!address) {
      this.snackbar.showWarning('Indirizzo spedizione non valido. Verifica i campi e riprova.');
      return false;
    }

    const countryCode = this.normalizeRequired(address.country).toUpperCase();
    if (!countryCode || countryCode.length !== 2) {
      this.snackbar.showWarning('Seleziona un paese valido.');
      return false;
    }

    if (!CheckoutComponent.supportedCountryCodes.has(countryCode)) {
      this.snackbar.showWarning('Paese non supportato. Usa IT, US o GB.');
      return false;
    }

    const region = this.normalizeOptional(address.state);
    if (
      CheckoutComponent.strictRegionCountryCodes.has(countryCode) &&
      region &&
      !/^[A-Za-z]{2}$/.test(region)
    ) {
      this.snackbar.showWarning(
        'Per IT/US il campo Provincia/Stato deve essere la sigla di 2 lettere (es. TO, MI, CA, NY).',
      );
      return false;
    }

    return true;
  }

  private showAddressSaveError(error: unknown): void {
    if (!(error instanceof HttpErrorResponse)) {
      this.snackbar.showError('Errore inatteso durante il salvataggio indirizzo.');
      return;
    }

    if (error.status === 401) {
      this.snackbar.showError('Sessione scaduta. Effettua di nuovo l’accesso e riprova.');
      return;
    }

    if (error.status === 404) {
      this.snackbar.showError('Utente non trovato. Ricarica la pagina e riprova.');
      return;
    }

    if (error.status === 400) {
      const validationMap = extractValidationErrorMap(error.error);
      if (validationMap) {
        const firstError = Object.values(validationMap).flat()[0];
        if (firstError) {
          this.snackbar.showWarning(firstError);
          return;
        }
      }

      this.snackbar.showWarning('Indirizzo non valido. Verifica i campi e riprova.');
      return;
    }

    this.snackbar.showError('Impossibile salvare l’indirizzo predefinito. Riprova.');
  }

  private async remountAddressElementFromAccount(): Promise<void> {
    this.addressElement?.destroy();
    // Non serve ricaricare da API qui, StripeService lo fa internamente.
    // Impostiamo saveAddress a true perché stiamo visualizzando i dati del DB.
    this.saveAddress = true;
    this.cdr.detectChanges();

    this.recreateAddressContainer();
    this.addressElement = await this.stripeService.createAddressElement(true);
    await this.mountAddressElement(this.addressElement);
    this.initialValueSnapshot = null;
    this.lastPolledValue = null;
    this.bindAddressChangeHandler();
    setTimeout(() => this.startAddressPolling(), 1000);
  }

  private normalizeRequired(value: string | null | undefined): string {
    return value?.trim() ?? '';
  }

  private normalizeOptional(value: string | null | undefined): string | null {
    const normalized = value?.trim();
    return normalized ? normalized : null;
  }

  private async mountAddressElement(addressElement: StripeAddressElement): Promise<void> {
    await this.waitForElementContainer('address-element');
    addressElement.mount('#address-element');
  }

  private async waitForElementContainer(
    elementId: string,
    maxAttempts = 20,
    delayMs = 50,
  ): Promise<void> {
    for (let attempt = 0; attempt < maxAttempts; attempt++) {
      if (document.getElementById(elementId)) {
        return;
      }

      await new Promise((resolve) => setTimeout(resolve, delayMs));
    }

    throw new Error(`Contenitore ${elementId} non disponibile.`);
  }

  private recreateAddressContainer(): void {
    this.recreateElementContainer('address-element');
  }

  private recreatePaymentContainer(): void {
    this.recreateElementContainer('payment-element');
  }

  private recreateElementContainer(elementId: string): void {
    const currentContainer = document.getElementById(elementId);
    if (!currentContainer || !currentContainer.parentElement) {
      return;
    }

    const replacement = document.createElement('div');
    replacement.id = elementId;
    currentContainer.parentElement.replaceChild(replacement, currentContainer);
  }

  private bindAddressChangeHandler(): void {
    // Polling handles changes now.
  }

  private async loadDefaultAddressSnapshotFromApi(): Promise<void> {
    try {
      await firstValueFrom(this.accountService.getAddress());
      this.saveAddress = true;
    } catch {
      this.saveAddress = false;
    }
  }

  private startAddressPolling(): void {
    this.stopAddressPolling();
    this.addressPollingTimerId = setInterval(() => {
      this.ngZone.run(() => {
        void this.checkAddressChange();
      });
    }, 300);
  }

  private stopAddressPolling(): void {
    if (this.addressPollingTimerId) {
      clearInterval(this.addressPollingTimerId);
      this.addressPollingTimerId = null;
    }
  }

  private async checkAddressChange(): Promise<void> {
    if (!this.addressElement) {
      return;
    }

    try {
      const result = await this.addressElement.getValue();
      const currentVal = JSON.stringify(result.value);

      if (this.initialValueSnapshot === null) {
        this.initialValueSnapshot = currentVal;
        this.lastPolledValue = currentVal;
        // Quando catturiamo lo snapshot iniziale, il checkbox deve essere spuntato
        // perché stiamo caricando i dati dal DB
        this.saveAddress = true;
        // Forza Angular a rilevare il cambiamento e aggiornare l'UI
        this.cdr.detectChanges();
        return;
      }

      // Se il valore è cambiato rispetto all'ultimo controllo (intervento utente sui dati)
      if (currentVal !== this.lastPolledValue) {
        this.lastPolledValue = currentVal;

        const shouldBeChecked = currentVal === this.initialValueSnapshot;

        // Aggiorniamo il checkbox solo se lo stato calcolato è diverso da quello attuale
        if (this.saveAddress !== shouldBeChecked) {
          this.saveAddress = shouldBeChecked;
          this.cdr.detectChanges();
        }
      }
    } catch {
      // Ignora errori durante il polling
    }
  }

  private async initializePaymentElementForStep(): Promise<void> {
    if (this.isLoadingPaymentElement || this.isProcessingPayment) {
      return;
    }

    this.isLoadingPaymentElement = true;
    this.paymentElementError = null;
    this.isPaymentElementComplete = false;
    this.isPaymentElementReady = false;

    try {
      await firstValueFrom(
        this.stripeService.createOrUpdatePaymentIntent({
          savePaymentMethod: this.savePaymentMethod,
          paymentMethodId: this.selectedSavedPaymentMethodId,
        }),
      );
      if (this.selectedSavedPaymentMethodId) {
        this.paymentElement?.destroy();
        this.paymentElement = undefined;
        this.isPaymentElementReady = true;
        this.isPaymentElementComplete = true;
        return;
      }

      this.recreatePaymentContainer();
      this.paymentElement = await this.stripeService.createPaymentElement(true);
      await this.mountPaymentElement(this.paymentElement);
      this.bindPaymentElementChangeHandler(this.paymentElement);
      this.isPaymentElementReady = true;
    } catch (error) {
      const message = this.getPaymentElementLoadErrorMessage(error);
      this.paymentElementError = message;
      this.snackbar.showError(message);
      this.isPaymentElementReady = false;
    } finally {
      this.isLoadingPaymentElement = false;
      this.cdr.detectChanges();
    }
  }

  private async mountPaymentElement(paymentElement: StripePaymentElement): Promise<void> {
    await this.waitForElementContainer('payment-element');
    paymentElement.mount('#payment-element');
  }

  private bindPaymentElementChangeHandler(paymentElement: StripePaymentElement): void {
    paymentElement.on('change', (event) => {
      this.ngZone.run(() => {
        this.isPaymentElementComplete = !!event.complete;
        this.paymentElementError = null;
        this.cdr.detectChanges();
      });
    });
  }

  async proceedToConfirmation(stepper: MatStepper): Promise<void> {
    if (this.isProcessingPayment) {
      return;
    }

    if (
      !this.selectedSavedPaymentMethodId &&
      (!this.isPaymentElementReady || !this.isPaymentElementComplete)
    ) {
      this.snackbar.showWarning('Completa i dati di pagamento prima di proseguire.');
      return;
    }

    const cart = this.cartService.cart();
    if (!cart?.id) {
      this.snackbar.showError('Carrello non disponibile. Ricarica la pagina e riprova.');
      return;
    }

    this.isProcessingPayment = true;
    this.paymentElementError = null;

    try {
      await firstValueFrom(
        this.stripeService.createOrUpdatePaymentIntent({
          savePaymentMethod: this.savePaymentMethod && !this.selectedSavedPaymentMethodId,
          paymentMethodId: this.selectedSavedPaymentMethodId,
        }),
      );

      const confirmation = await this.stripeService.confirmPayment(
        this.selectedSavedPaymentMethodId ?? undefined,
      );
      if (!confirmation.isSuccess) {
        this.paymentElementError =
          confirmation.message ?? 'Pagamento non riuscito. Verifica i dati e riprova.';
        return;
      }

      const finalizeResult = await firstValueFrom(
        this.stripeService.finalizePayment(cart.id, confirmation.paymentIntentId),
      );

      if (!finalizeResult.isSuccess) {
        this.paymentElementError =
          finalizeResult.message ?? 'Pagamento non finalizzato lato server. Riprova.';
        return;
      }

      try {
        this.checkoutService.resetCheckoutStateAfterPayment(cart.id);
        this.cartService.clearClientCartState();
      } catch {
        this.snackbar.showWarning(
          'Pagamento confermato, ma il reset locale del carrello non è completo. Ricarica la pagina.',
        );
      }

      this.isOrderConfirmed = true;
      this.confirmedOrderId = finalizeResult.orderId ?? null;
      this.confirmationMessage = finalizeResult.message ?? null;
      this.ordersService.invalidateOrdersCache();
      await this.loadSavedPaymentMethods();
      this.snackbar.showInfo('Pagamento confermato con successo.');
      stepper.next();
    } catch (error) {
      this.paymentElementError = this.extractPaymentErrorMessage(error);
    } finally {
      this.isProcessingPayment = false;
      this.cdr.detectChanges();
    }
  }

  private extractPaymentErrorMessage(error: unknown): string {
    if (!(error instanceof HttpErrorResponse)) {
      return 'Errore inatteso durante la conferma del pagamento.';
    }

    const apiMessage = typeof error.error?.message === 'string' ? error.error.message : null;

    if (apiMessage) {
      return apiMessage;
    }

    if (error.status === 409) {
      return 'Pagamento non ancora confermato. Riprova tra qualche secondo.';
    }

    if (error.status === 502) {
      return 'Errore temporaneo del provider di pagamento. Riprova.';
    }

    if (error.status === 403) {
      return 'Operazione non autorizzata sul pagamento corrente.';
    }

    return 'Impossibile finalizzare il pagamento. Riprova.';
  }

  retryPaymentElementLoad(): void {
    void this.initializePaymentElementForStep();
  }

  private getPaymentElementLoadErrorMessage(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      const apiMessage =
        typeof error.error?.details === 'string'
          ? error.error.details
          : typeof error.error?.message === 'string'
            ? error.error.message
            : null;

      if (apiMessage) {
        return apiMessage;
      }

      if (error.status === 404) {
        return 'Carrello non trovato o scaduto. Torna al carrello e riprova.';
      }

      if (error.status === 400) {
        return 'Dati carrello non validi per inizializzare il pagamento.';
      }

      if (error.status === 502) {
        return 'Errore Stripe durante inizializzazione pagamento. Riprova tra poco.';
      }

      return error.message || 'Errore HTTP durante il caricamento del pagamento.';
    }

    if (!(error instanceof Error)) {
      return 'Errore inatteso durante il caricamento del pagamento.';
    }

    if (error.message.includes('Client secret Stripe mancante')) {
      return 'Sessione pagamento non pronta. Torna a Shipping e riprova.';
    }

    return error.message;
  }
}
