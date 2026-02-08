import { effect, inject, Injectable, signal } from '@angular/core';
import { environment } from '../../../environments/environment.development';
import { HttpClient } from '@angular/common/http';
import { DeliveryMethod } from '../../shared/models/deliveryMethod';
import { map, of } from 'rxjs';
import { AuthStateService } from './auth-state.service';
import { CartService } from './cart.service';

@Injectable({
  providedIn: 'root',
})
export class CheckoutService {
  private readonly guestShippingStorageKey = 'checkout_shipping_guest';
  private readonly shippingStorageValueVersion = 1;
  private previousShippingContextKey: string | null = null;
  baseUrl = environment.apiUrl;
  private http = inject(HttpClient);
  private authState = inject(AuthStateService);
  private cartService = inject(CartService);
  deliveryMethods: DeliveryMethod[] = [];
  selectedDeliveryMethod = signal<DeliveryMethod | null>(null);

  constructor() {
    effect(() => {
      const userEmail = this.authState.user()?.email ?? null;
      const cartId = this.cartService.cart()?.id ?? null;
      const currentContextKey = this.getShippingStorageKeyForContext(userEmail, cartId);

      if (this.previousShippingContextKey === null) {
        this.previousShippingContextKey = currentContextKey;
        return;
      }

      if (this.previousShippingContextKey === currentContextKey) {
        return;
      }

      this.previousShippingContextKey = currentContextKey;
      this.selectedDeliveryMethod.set(null);

      if (this.deliveryMethods.length > 0) {
        this.restoreSelectedDeliveryMethodFromStorage();
      }
    });
  }

  getDeliveryMethods() {
    if (this.deliveryMethods.length > 0) {
      this.restoreSelectedDeliveryMethodFromStorage();
      return of(this.deliveryMethods);
    }

    return this.http.get<DeliveryMethod[]>(this.baseUrl + 'payments/delivery-methods').pipe(
      map((methods) => {
        this.deliveryMethods = methods.sort((a, b) => b.price - a.price);
        this.restoreSelectedDeliveryMethodFromStorage();
        return this.deliveryMethods;
      }),
    );
  }

  selectDeliveryMethodById(deliveryMethodId: number): void {
    const selectedMethod = this.deliveryMethods.find((method) => method.id === deliveryMethodId) ?? null;
    this.selectedDeliveryMethod.set(selectedMethod);
    this.persistSelectedDeliveryMethodId(selectedMethod?.id ?? null);
    this.syncCartDeliveryMethodId(selectedMethod?.id ?? null);
  }

  restoreSelectedDeliveryMethodFromStorage(): void {
    const storageKey = this.getShippingStorageKeyForCurrentContext();
    const raw = localStorage.getItem(storageKey);
    if (!raw) {
      this.selectedDeliveryMethod.set(null);
      this.syncCartDeliveryMethodId(null);
      return;
    }

    try {
      const parsed = JSON.parse(raw) as { deliveryMethodId?: unknown };
      const deliveryMethodId =
        typeof parsed?.deliveryMethodId === 'number' ? parsed.deliveryMethodId : null;

      if (deliveryMethodId === null) {
        this.selectedDeliveryMethod.set(null);
        localStorage.removeItem(storageKey);
        this.syncCartDeliveryMethodId(null);
        return;
      }

      const selectedMethod = this.deliveryMethods.find((method) => method.id === deliveryMethodId) ?? null;
      if (!selectedMethod) {
        this.selectedDeliveryMethod.set(null);
        localStorage.removeItem(storageKey);
        this.syncCartDeliveryMethodId(null);
        return;
      }

      this.selectedDeliveryMethod.set(selectedMethod);
      this.syncCartDeliveryMethodId(selectedMethod.id);
    } catch {
      this.selectedDeliveryMethod.set(null);
      localStorage.removeItem(storageKey);
      this.syncCartDeliveryMethodId(null);
    }
  }

  resetCheckoutStateAfterPayment(cartId: string): void {
    const userEmail = this.authState.user()?.email ?? null;
    const storageKey = this.getShippingStorageKeyForContext(userEmail, cartId);

    this.selectedDeliveryMethod.set(null);
    localStorage.removeItem(storageKey);
    this.syncCartDeliveryMethodId(null);
  }

  private persistSelectedDeliveryMethodId(deliveryMethodId: number | null): void {
    const storageKey = this.getShippingStorageKeyForCurrentContext();
    if (deliveryMethodId === null) {
      localStorage.removeItem(storageKey);
      return;
    }

    const payload = {
      v: this.shippingStorageValueVersion,
      deliveryMethodId,
    };

    localStorage.setItem(storageKey, JSON.stringify(payload));
  }

  private syncCartDeliveryMethodId(deliveryMethodId: number | null): void {
    const currentCart = this.cartService.cart();
    if (!currentCart) {
      return;
    }

    const normalizedDeliveryMethodId = deliveryMethodId ?? undefined;
    if (currentCart.deliveryMethodId === normalizedDeliveryMethodId) {
      return;
    }

    this.cartService.cart.set({
      ...currentCart,
      deliveryMethodId: normalizedDeliveryMethodId,
    });
  }

  getShippingStorageKeyForCurrentContext(): string {
    const userEmail = this.authState.user()?.email;
    const cartId = this.cartService.cart()?.id;
    return this.getShippingStorageKeyForContext(userEmail ?? null, cartId ?? null);
  }

  private getShippingStorageKeyForContext(
    userEmail: string | null,
    cartId: string | null,
  ): string {
    if (!userEmail || !cartId) {
      return this.guestShippingStorageKey;
    }

    return this.buildUserCartShippingStorageKey(userEmail, cartId);
  }

  private buildUserCartShippingStorageKey(email: string, cartId: string): string {
    const normalizedEmail = email
      .trim()
      .toLowerCase()
      .replace(/[^a-z0-9._-]/g, '_');

    const normalizedCartId = cartId
      .trim()
      .toLowerCase()
      .replace(/[^a-z0-9._-]/g, '_');

    return `checkout_shipping_user_${normalizedEmail}_cart_${normalizedCartId}`;
  }
}
