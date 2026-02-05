import { Component } from '@angular/core';
import { OrderSummaryComponent } from '../../shared/components/order-summary/order-summary.component';
import { MATERIAL_IMPORTS } from '../../shared/material';

@Component({
  selector: 'app-checkout',
  imports: [OrderSummaryComponent, ...MATERIAL_IMPORTS],
  standalone: true,
  templateUrl: './checkout.component.html',
  styleUrl: './checkout.component.scss',
})
export class CheckoutComponent {}
