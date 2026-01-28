import { Component, inject, OnInit } from '@angular/core';
import { ShopService } from '../../../core/services/shop.service';
import { ActivatedRoute } from '@angular/router';
import { Product } from '../../../shared/models/product';

@Component({
  selector: 'app-product-details',
  standalone: true,
  imports: [],
  templateUrl: './product-details.component.html',
  styleUrl: './product-details.component.scss',
})
export class ProductDetailsComponent implements OnInit {
  private shopService = inject(ShopService);
  private activateRoute = inject(ActivatedRoute);
  product?: Product;
  isLoading = false;
  errorMessage = '';

  ngOnInit(): void {
    this.loadProduct(); 
  }

  loadProduct() {
    //  legge il parametro di route id dall’URL corrente.
    // n pratica prende l’id dai parametri della route (es. /shop/123)
    // usando lo snapshot dell’ActivatedRoute.
    const id = this.activateRoute.snapshot.paramMap.get('id');
    if (!id) {
      this.errorMessage = 'Id prodotto mancante.';
      return;
    }
    // +id è un trucco JavaScript per convertire una stringa in numero (operatore unario +).
    this.isLoading = true;
    this.errorMessage = '';
    this.shopService.getProduct(+id).subscribe({
      next: (prod) => (this.product = prod),
      error: () => {
        this.errorMessage = 'Prodotto non trovato o servizio non disponibile.';
      },
      complete: () => {
        this.isLoading = false;
      },
    });
  }
}
