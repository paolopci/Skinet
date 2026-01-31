import { Component, computed, inject } from '@angular/core';
import { CartService } from '../../core/services/cart.service';
import { MATERIAL_IMPORTS } from '../../shared/material';
import { CurrencyPipe } from '@angular/common';
import { CartItem } from '../../shared/models/cart';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-cart',
  imports: [MATERIAL_IMPORTS, CurrencyPipe, RouterLink],
  templateUrl: './cart.component.html',
  styleUrl: './cart.component.scss',
})
export class CartComponent {
  cartService = inject(CartService);
  subtotal = computed(
    () =>
      this.cartService
        .cart()
        ?.items.reduce((total, item) => total + item.price * item.quantity, 0) ?? 0,
  );
  shippingCost = computed(() => (this.subtotal() > 0 ? 0 : 0));
  discount = computed(() => 0);
  orderTotal = computed(() => this.subtotal() + this.shippingCost() - this.discount());

  increase(item: CartItem) {
    this.cartService.incrementItem(item.productId);
  }

  decrease(item: CartItem) {
    this.cartService.decrementItem(item.productId);
  }

  onQuantityInput(item: CartItem, event: Event) {
    const value = Number((event.target as HTMLInputElement).value);
    this.cartService.setItemQuantity(item.productId, value);
  }
}
