import { computed, inject, Injectable, signal } from '@angular/core';
import { environment } from '../../../environments/environment.development';
import { HttpClient } from '@angular/common/http';
import { Cart, CartItem } from '../../shared/models/cart';
import { Product } from '../../shared/models/product';

@Injectable({
  providedIn: 'root',
})
export class CartService {
  private readonly cartIdKey = 'cart_id';
  baseUrl = environment.apiUrl;
  private http = inject(HttpClient);

  cart = signal<Cart | null>(null);
  // Totale pezzi nel carrello (somma delle quantita per item).
  totalItemsCount = computed(
    () => this.cart()?.items.reduce((total, item) => total + item.quantity, 0) ?? 0,
  );

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

  private createCart() {
    const cart = new Cart();
    this.updateCartState(cart);
    return cart;
  }

  private mapProductToCartItem(product: Product, quantity: number): CartItem {
    return {
      productId: product.id,
      ProductName: product.name,
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
