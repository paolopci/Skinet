import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Product } from '../../shared/models/product';
import { Pagination } from '../../shared/models/pagination';
import { map } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class ShopService {
  types: string[] = [];
  brands: string[] = [];

  baseUrl = 'https://localhost:5001/api/';

  private http = inject(HttpClient);

  getProducts() {
    return this.http
      .get<Pagination<Product> | Product[]>(this.baseUrl + 'products?pageSize=20')
      .pipe(
        map((response) => {
          if (Array.isArray(response)) {
            return response;
          }

          const data =
            (response as { data?: unknown; items?: unknown }).data ??
            (response as { Data?: unknown; Items?: unknown }).Data ??
            (response as { data?: unknown; items?: unknown }).items ??
            (response as { Data?: unknown; Items?: unknown }).Items;

          return Array.isArray(data) ? data : [];
        }),
      );
  }

  getBrands() {
    // se hai già types in memoria non rifaccio la chiamata
    // facciamo questa chiamata una volta sola all'avvio dell'applicazione
    if (this.brands.length > 0) return;
    return this.http.get<string[]>(this.baseUrl + 'products/brands').subscribe({
      next: (response) => (this.brands = response),
    });
  }

  getTypes() {
    // se hai già types in memoria non rifaccio la chiamata
    // facciamo questa chiamata una volta sola all'avvio dell'applicazione
    if (this.types.length > 0) return;
    return this.http.get<string[]>(this.baseUrl + 'products/types').subscribe({
      next: (response) => (this.types = response),
    });
  }
}
