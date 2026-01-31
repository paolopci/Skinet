import { Component, inject, Input } from '@angular/core';
import { Product } from '../../../shared/models/product';
import { MATERIAL_IMPORTS } from '../../../shared/material';
import { CurrencyPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { CartService } from '../../../core/services/cart.service';

@Component({
  selector: 'app-product-item',
  standalone: true,
  imports: [MATERIAL_IMPORTS, CurrencyPipe, RouterLink],
  templateUrl: './product-item.component.html',
  styleUrl: './product-item.component.scss',
})
export class ProductItemComponent {
  @Input() product?: Product;
  private cartService = inject(CartService);

  // Aggiunge l'articolo selezionato al carrello.
  addToCart() {
    if (!this.product) {
      return;
    }

    this.cartService.addItemToCart(this.product);
  }
}
