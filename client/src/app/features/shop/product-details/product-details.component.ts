import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { catchError, EMPTY, finalize, switchMap, tap } from 'rxjs';
import { ShopService } from '../../../core/services/shop.service';
import { Product } from '../../../shared/models/product';
import { CurrencyPipe } from '@angular/common';
import { MATERIAL_IMPORTS } from '../../../shared/material';

@Component({
  selector: 'app-product-details',
  standalone: true,
  imports: [CurrencyPipe, RouterLink, ...MATERIAL_IMPORTS],
  templateUrl: './product-details.component.html',
  styleUrl: './product-details.component.scss',
})
export class ProductDetailsComponent implements OnInit {
  private shopService = inject(ShopService);
  private activateRoute = inject(ActivatedRoute);

  product = signal<Product | undefined>(undefined);
  isLoading = signal(false);
  errorMessage = signal('');

  ngOnInit(): void {
    this.activateRoute.paramMap
      .pipe(
        switchMap((params) => {
          const id = params.get('id');
          if (!id) {
            this.errorMessage.set('Id prodotto mancante.');
            this.product.set(undefined);
            return EMPTY;
          }

          this.isLoading.set(true);
          this.errorMessage.set('');
          this.product.set(undefined);

          return this.shopService.getProduct(+id).pipe(
            tap((prod) => {
              this.product.set(prod);
            }),
            catchError((error) => {
              console.error('Error fetching product', error);
              this.errorMessage.set('Prodotto non trovato o servizio non disponibile.');
              return EMPTY;
            }),
            finalize(() => {
              this.isLoading.set(false);
            }),
          );
        }),
      )
      .subscribe();
  }
}
