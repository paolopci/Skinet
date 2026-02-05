import { Component, computed, inject, OnInit } from '@angular/core';
import { OrderSummaryComponent } from '../../shared/components/order-summary/order-summary.component';
import { MATERIAL_IMPORTS } from '../../shared/material';
import { CartService } from '../../core/services/cart.service';
import { StripeService } from '../../core/services/stripe.service';
import { StripeAddressElement } from '@stripe/stripe-js';
import { SnackbarService } from '../../core/services/snackbar.service';

@Component({
  selector: 'app-checkout',
  imports: [OrderSummaryComponent, ...MATERIAL_IMPORTS],
  standalone: true,
  templateUrl: './checkout.component.html',
  styleUrl: './checkout.component.scss',
})
export class CheckoutComponent implements OnInit {
  private stripeService = inject(StripeService);
  private snackbar = inject(SnackbarService);
  cartService = inject(CartService);
  addressElement?: StripeAddressElement;

  subtotal = computed(
    () =>
      this.cartService
        .cart()
        ?.items.reduce((total, item) => total + item.price * item.quantity, 0) ?? 0,
  );
  shippingCost = computed(() => (this.subtotal() > 0 ? 0 : 0));
  discount = computed(() => 0);
  orderTotal = computed(() => this.subtotal() + this.shippingCost() - this.discount());

  async ngOnInit() {
    try {
      this.addressElement = await this.stripeService.createAddressElement();
      this.addressElement.mount('#address-element');
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Errore inatteso.';
      this.snackbar.showError(message);
    }
  }
}
