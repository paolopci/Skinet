import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable, of } from 'rxjs';
import { environment } from '../../../environments/environment.development';
import { OrderListItem } from '../../shared/models/order-list-item';
import { OrdersQueryParams } from '../../shared/models/orders-query-params';
import { OrdersResponse } from '../../shared/models/orders-response';

@Injectable({
  providedIn: 'root',
})
export class OrdersService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  private hasOrdersCache: boolean | null = null;

  getOrders(params: OrdersQueryParams): Observable<OrdersResponse> {
    const apiParams = new HttpParams().set('sortBy', params.sortBy).set('order', params.order);

    return this.http
      .get<unknown>(`${this.baseUrl}orders`, { params: apiParams })
      .pipe(map((response) => this.toOrdersResponse(response, params)));
  }

  hasOrders(): Observable<boolean> {
    if (this.hasOrdersCache !== null) {
      return of(this.hasOrdersCache);
    }

    const params: OrdersQueryParams = {
      sortBy: 'dataordine',
      order: 'desc',
      quarter: null,
      year: null,
      search: '',
      currentPage: 1,
      pageSize: 10,
    };

    return this.getOrders(params).pipe(
      map((response) => {
        const hasOrders = response.pagination.totalOrders > 0;
        this.hasOrdersCache = hasOrders;
        return hasOrders;
      }),
    );
  }

  invalidateOrdersCache(): void {
    this.hasOrdersCache = null;
  }

  private toOrdersResponse(rawResponse: unknown, params: OrdersQueryParams): OrdersResponse {
    const rawItems = this.extractRawItems(rawResponse);
    const mapped = rawItems.map((item) => this.mapOrderItem(item)).filter((item) => !!item);
    const normalizedOrders = mapped as OrderListItem[];
    const filteredOrders = this.applyClientFilters(normalizedOrders, params);
    const safePageSize = params.pageSize > 0 ? params.pageSize : 10;
    const totalOrders = filteredOrders.length;
    const totalPages = totalOrders === 0 ? 0 : Math.ceil(totalOrders / safePageSize);
    const maxPage = totalPages === 0 ? 1 : totalPages;
    const currentPage = Math.min(Math.max(params.currentPage || 1, 1), maxPage);
    const start = (currentPage - 1) * safePageSize;
    const pagedOrders = filteredOrders.slice(start, start + safePageSize);

    return {
      orders: pagedOrders,
      pagination: {
        currentPage,
        pageSize: safePageSize,
        totalPages,
        totalOrders,
      },
    };
  }

  private applyClientFilters(orders: OrderListItem[], params: OrdersQueryParams): OrderListItem[] {
    const normalizedSearch = this.normalizeSearch(params.search);
    const normalizedSearchDate = this.normalizeSearchDate(params.search);

    return orders.filter((order) => {
      const quarterMatch = params.quarter === null || order.trimestre === params.quarter;
      const yearMatch = params.year === null || order.anno === params.year;
      const searchMatch =
        normalizedSearch.length === 0 ||
        order.orderId.toLowerCase().includes(normalizedSearch) ||
        order.data.includes(normalizedSearch) ||
        (normalizedSearchDate !== null && order.data === normalizedSearchDate);

      return quarterMatch && yearMatch && searchMatch;
    });
  }

  private extractRawItems(rawResponse: unknown): Record<string, unknown>[] {
    if (Array.isArray(rawResponse)) {
      return rawResponse as Record<string, unknown>[];
    }

    if (!rawResponse || typeof rawResponse !== 'object') {
      return [];
    }

    const responseObject = rawResponse as Record<string, unknown>;
    const candidate =
      responseObject['orders'] ??
      responseObject['Orders'] ??
      responseObject['data'] ??
      responseObject['Data'] ??
      responseObject['items'] ??
      responseObject['Items'];

    return Array.isArray(candidate) ? (candidate as Record<string, unknown>[]) : [];
  }

  private mapOrderItem(rawItem: Record<string, unknown>): OrderListItem | null {
    const orderIdRaw = this.getStringValue(rawItem, ['orderId', 'OrderId']);
    const dataRaw = this.getStringValue(rawItem, ['data', 'Data', 'dataOrdine', 'DataOrdine']);
    const statoRaw = this.getStringValue(rawItem, ['stato', 'Stato', 'statoOrdine', 'StatoOrdine']);
    const importoRaw = this.getNumberValue(rawItem, [
      'importo',
      'Importo',
      'totaleOrdine',
      'TotaleOrdine',
    ]);

    const normalizedDate = this.normalizeDateToIso(dataRaw);
    const parsedDate = this.parseDate(normalizedDate);

    if (!orderIdRaw || !parsedDate) {
      return null;
    }

    const month = parsedDate.getUTCMonth() + 1;
    return {
      orderId: orderIdRaw,
      data: normalizedDate ?? parsedDate.toISOString().slice(0, 10),
      stato: statoRaw ?? '',
      importo: importoRaw ?? 0,
      trimestre: Math.floor((month - 1) / 3) + 1,
      anno: parsedDate.getUTCFullYear(),
    };
  }

  private getStringValue(source: Record<string, unknown>, keys: string[]): string | null {
    for (const key of keys) {
      const value = source[key];
      if (typeof value === 'string' && value.trim().length > 0) {
        return value.trim();
      }
      if (typeof value === 'number') {
        return value.toString();
      }
    }

    return null;
  }

  private getNumberValue(source: Record<string, unknown>, keys: string[]): number | null {
    for (const key of keys) {
      const value = source[key];
      if (typeof value === 'number' && Number.isFinite(value)) {
        return value;
      }
      if (typeof value === 'string') {
        const parsed = Number(value.replace(',', '.'));
        if (Number.isFinite(parsed)) {
          return parsed;
        }
      }
    }

    return null;
  }

  private parseDate(value: string | null): Date | null {
    if (!value) {
      return null;
    }

    const parsed = new Date(value);
    if (Number.isNaN(parsed.getTime())) {
      return null;
    }

    return parsed;
  }

  private normalizeDateToIso(value: string | null): string | null {
    if (!value) {
      return null;
    }

    const trimmed = value.trim();
    const isoMatch = /^(\d{4})-(\d{2})-(\d{2})/.exec(trimmed);
    if (isoMatch) {
      return `${isoMatch[1]}-${isoMatch[2]}-${isoMatch[3]}`;
    }

    const localMatch = /^(\d{2})\/(\d{2})\/(\d{4})$/.exec(trimmed);
    if (localMatch) {
      return `${localMatch[3]}-${localMatch[2]}-${localMatch[1]}`;
    }

    const parsed = this.parseDate(trimmed);
    return parsed ? parsed.toISOString().slice(0, 10) : null;
  }

  private normalizeSearch(value: string): string {
    return value.trim().toLowerCase();
  }

  private normalizeSearchDate(value: string): string | null {
    const trimmed = value.trim();
    if (!trimmed) {
      return null;
    }

    return this.normalizeDateToIso(trimmed);
  }
}
