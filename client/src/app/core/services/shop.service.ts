import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Product } from '../../shared/models/product';
import { Pagination } from '../../shared/models/pagination';
import { ShopParams } from '../../shared/models/shop-params';
import { map } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class ShopService {
  types: string[] = [];
  brands: string[] = [];

  baseUrl = 'https://localhost:5001/api/';

  private http = inject(HttpClient);

  getProducts(params: ShopParams) {
    let httpParams = new HttpParams()
      .set('pageIndex', params.pageIndex)
      .set('pageSize', params.pageSize)
      .set('sort', params.sort);

    if (params.brands.length > 0) {
      httpParams = httpParams.set('brands', params.brands.join(','));
    }

    if (params.types.length > 0) {
      httpParams = httpParams.set('types', params.types.join(','));
    }

    if (params.search.trim().length > 0) {
      httpParams = httpParams.set('search', params.search);
    }

    return this.http
      .get<Pagination<Product> | Product[]>(this.baseUrl + 'products', {
        params: httpParams,
      })
      .pipe(
        map((response) => {
          if (Array.isArray(response)) {
            return {
              pageIndex: params.pageIndex,
              pageSize: params.pageSize,
              totalCount: response.length,
              totalPages: 1,
              data: response,
            } as Pagination<Product>;
          }

          const responseData =
            (response as { data?: unknown; items?: unknown }).data ??
            (response as { Data?: unknown; Items?: unknown }).Data ??
            (response as { data?: unknown; items?: unknown }).items ??
            (response as { Data?: unknown; Items?: unknown }).Items;

          const data = Array.isArray(responseData) ? responseData : [];

          return {
            pageIndex:
              (response as { pageIndex?: number; PageIndex?: number }).pageIndex ??
              (response as { PageIndex?: number }).PageIndex ??
              params.pageIndex,
            pageSize:
              (response as { pageSize?: number; PageSize?: number }).pageSize ??
              (response as { PageSize?: number }).PageSize ??
              params.pageSize,
            totalCount:
              (response as { totalCount?: number; TotalCount?: number }).totalCount ??
              (response as { TotalCount?: number }).TotalCount ??
              data.length,
            totalPages:
              (response as { totalPages?: number; TotalPages?: number }).totalPages ??
              (response as { TotalPages?: number }).TotalPages ??
              1,
            data,
          } as Pagination<Product>;
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
