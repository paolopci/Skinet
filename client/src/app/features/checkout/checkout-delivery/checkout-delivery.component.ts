import { Component, inject, OnInit } from '@angular/core';
import { CheckoutService } from '../../../core/services/checkout.service';
import { MATERIAL_IMPORTS } from '../../../shared/material';
import { MatRadioButton, MatRadioChange } from '@angular/material/radio';
import { CurrencyPipe } from '@angular/common';

@Component({
  selector: 'app-checkout-delivery',
  standalone: true,
  imports: [MatRadioButton, CurrencyPipe, ...MATERIAL_IMPORTS],
  templateUrl: './checkout-delivery.component.html',
  styleUrl: './checkout-delivery.component.scss',
})
export class CheckoutDeliveryComponent implements OnInit {
  checkoutService = inject(CheckoutService);

  ngOnInit(): void {
    this.checkoutService.getDeliveryMethods().subscribe();
  }

  onDeliveryMethodChange(event: MatRadioChange): void {
    this.checkoutService.selectDeliveryMethodById(event.value);
  }
}
