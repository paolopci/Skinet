import { CurrencyPipe } from '@angular/common';
import { Component, Input } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-order-summary',
  imports: [CurrencyPipe, RouterLink],
  templateUrl: './order-summary.component.html',
  styleUrl: './order-summary.component.scss',
})
export class OrderSummaryComponent {
  @Input() subtotal = 0;
  @Input() discount = 0;
  @Input() shippingCost = 0;
  @Input() orderTotal = 0;
}
