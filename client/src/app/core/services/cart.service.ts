import { computed, inject, Injectable, signal } from '@angular/core';
import { environment } from '../../../environments/environment.development';
import { HttpClient } from '@angular/common/http';
import { Cart, CartItem } from '../../shared/models/cart';
import { Product } from '../../shared/models/product';
import { SnackbarService } from './snackbar.service';

@Injectable({
  providedIn: 'root',
})
export class CartService {
  private readonly cartIdKey = 'cart_id';
  baseUrl = environment.apiUrl;
  private http = inject(HttpClient);
  private snackbar = inject(SnackbarService);

  cart = signal<Cart | null>(null);
  // Totale pezzi nel carrello (somma delle quantita per item).
  totalItemsCount = computed(
    () => this.cart()?.items.reduce((total, item) => total + item.quantity, 0) ?? 0,
  );

  // Carica il carrello persistito (se presente) all'avvio dell'app.
  loadCart() {
    const cartId = localStorage.getItem(this.cartIdKey);
    if (!cartId) {
      return;
    }

    this.getCart(cartId);
  }

  getCart(id: string) {
    return this.http.get<Cart>(`${this.baseUrl}cart?id=${id}`).subscribe({
      next: (cart) => {
        this.updateCartState(cart);
      },
      error: (error) => {
        console.log(error);
      },
    });
  }

  setCart(cart: Cart) {
    return this.http.post<Cart>(`${this.baseUrl}cart`, cart).subscribe({
      next: (updatedCart) => {
        this.updateCartState(updatedCart);
      },
      error: (error) => {
        console.log(error);
      },
    });
  }

  deleteCart(id: string) {
    return this.http.delete(`${this.baseUrl}cart?id=${id}`).subscribe({
      next: () => {
        this.updateCartState(null);
      },
      error: (error) => {
        console.log(error);
      },
    });
  }

  mergeCart(guestCartId: string) {
    return this.http
      .post<Cart>(`${this.baseUrl}cart/merge`, { guestCartId })
      .subscribe({
        next: (updatedCart) => {
          this.updateCartState(updatedCart);
          this.snackbar.showInfo('Carrello sincronizzato');
        },
        error: (error) => {
          console.log(error);
        },
      });
  }

  // Aggiunge o incrementa un item, poi sincronizza con API.
  addItemToCart(product: Product, quantity = 1) {
    const safeQuantity = this.normalizeQuantity(quantity);
    const currentCart = this.cart() ?? this.createCart();
    const existingItem = currentCart.items.find((item) => item.productId === product.id);

    if (existingItem) {
      existingItem.quantity += safeQuantity;
    } else {
      currentCart.items.push(this.mapProductToCartItem(product, safeQuantity));
    }

    this.cart.set(currentCart);
    this.setCart(currentCart);
  }

  // Incrementa la quantita di un prodotto esistente.
  incrementItem(productId: number) {
    this.changeItemQuantity(productId, 1);
  }

  // Decrementa la quantita e rimuove l'item se arriva a zero.
  decrementItem(productId: number) {
    this.changeItemQuantity(productId, -1);
  }

  // Imposta una quantita specifica; se 0 rimuove il prodotto.
  setItemQuantity(productId: number, quantity: number) {
    const currentCart = this.cart();
    if (!currentCart) {
      return;
    }

    const existingItem = currentCart.items.find((item) => item.productId === productId);
    if (!existingItem) {
      return;
    }

    const normalized = this.normalizeQuantityAllowZero(quantity);
    if (normalized === 0) {
      this.removeItem(productId);
      return;
    }

    existingItem.quantity = normalized;
    this.persistCart(currentCart);
  }

  // Rimuove un prodotto dal carrello.
  removeItem(productId: number) {
    const currentCart = this.cart();
    if (!currentCart) {
      return;
    }

    currentCart.items = currentCart.items.filter((item) => item.productId !== productId);
    this.persistCart(currentCart);
  }

  private createCart() {
    const cart = new Cart();
    this.updateCartState(cart);
    return cart;
  }

  private mapProductToCartItem(product: Product, quantity: number): CartItem {
    return {
      productId: product.id,
      productName: product.name,
      price: product.price,
      quantity,
      pictureUrl: product.pictureUrl,
      brand: product.brand,
      type: product.type,
    };
  }

  private normalizeQuantity(quantity: number) {
    if (!Number.isFinite(quantity)) {
      return 1;
    }

    const normalized = Math.floor(quantity);
    return normalized > 0 ? normalized : 1;
  }

  private normalizeQuantityAllowZero(quantity: number) {
    if (!Number.isFinite(quantity)) {
      return 1;
    }

    const normalized = Math.floor(quantity);
    return normalized < 0 ? 0 : normalized;
  }

  private changeItemQuantity(productId: number, delta: number) {
    const currentCart = this.cart();
    if (!currentCart) {
      return;
    }

    const existingItem = currentCart.items.find((item) => item.productId === productId);
    if (!existingItem) {
      return;
    }

    const newQuantity = existingItem.quantity + delta;
    if (newQuantity <= 0) {
      this.removeItem(productId);
      return;
    }

    existingItem.quantity = newQuantity;
    this.persistCart(currentCart);
  }

  private persistCart(cart: Cart) {
    this.cart.set(cart);
    this.setCart(cart);
  }

  private updateCartState(cart: Cart | null) {
    this.cart.set(cart);

    // Persistenza locale dell'id carrello per recupero successivo.
    if (cart?.id) {
      localStorage.setItem(this.cartIdKey, cart.id);
    } else {
      localStorage.removeItem(this.cartIdKey);
    }
  }
}
