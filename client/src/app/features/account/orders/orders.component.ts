import { Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { CurrencyPipe } from '@angular/common';
import { MatDialog } from '@angular/material/dialog';
import { PageEvent } from '@angular/material/paginator';
import { MATERIAL_IMPORTS } from '../../../shared/material';
import { OrdersService } from '../../../core/services/orders.service';
import { OrdersQueryParams } from '../../../shared/models/orders-query-params';
import { OrdersResponse } from '../../../shared/models/orders-response';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import {
  OrderDetailsDialogComponent,
  OrderDetailsDialogData,
} from './order-details-dialog/order-details-dialog.component';

@Component({
  selector: 'app-orders',
  standalone: true,
  imports: [FormsModule, CurrencyPipe, ...MATERIAL_IMPORTS],
  templateUrl: './orders.component.html',
  styleUrl: './orders.component.scss',
})
export class OrdersComponent {
  private readonly ordersService = inject(OrdersService);
  private readonly dialogService = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);
  private readonly searchInput$ = new Subject<string>();

  readonly query = signal<OrdersQueryParams>({
    sortBy: 'dataordine',
    order: 'desc',
    quarter: null,
    year: null,
    search: '',
    currentPage: 1,
    pageSize: 10,
  });
  readonly response = signal<OrdersResponse>({
    orders: [],
    pagination: {
      currentPage: 1,
      pageSize: 10,
      totalPages: 0,
      totalOrders: 0,
    },
  });
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly hasOrders = computed(() => this.response().pagination.totalOrders > 0);
  readonly hasActiveFilters = computed(() => {
    const q = this.query();
    return q.quarter !== null || q.year !== null || q.search.trim().length > 0;
  });
  readonly years = computed(() => {
    const currentYear = new Date().getFullYear();
    const initialYears = [currentYear, currentYear - 1, currentYear - 2];
    const mappedYears = this.response().orders.map((item) => item.anno);
    return [...new Set([...mappedYears, ...initialYears])].sort((a, b) => b - a);
  });

  ngOnInit(): void {
    this.searchInput$
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe((value) => {
        this.query.update((prev) => ({
          ...prev,
          search: value,
          currentPage: 1,
        }));
        this.loadOrders();
      });

    this.loadOrders();
  }

  onSortOrderChange(value: 'asc' | 'desc'): void {
    this.query.update((prev) => ({
      ...prev,
      order: value,
      currentPage: 1,
    }));
    this.loadOrders();
  }

  onQuarterChange(value: string): void {
    const quarter = value === '' ? null : Number(value);
    this.query.update((prev) => ({
      ...prev,
      quarter: Number.isNaN(quarter) ? null : quarter,
      currentPage: 1,
    }));
    this.loadOrders();
  }

  onYearChange(value: string): void {
    const year = value === '' ? null : Number(value);
    this.query.update((prev) => ({
      ...prev,
      year: Number.isNaN(year) ? null : year,
      currentPage: 1,
    }));
    this.loadOrders();
  }

  onSearchChange(value: string): void {
    this.searchInput$.next(value);
  }

  onPageChange(event: PageEvent): void {
    this.query.update((prev) => ({
      ...prev,
      currentPage: event.pageIndex + 1,
      pageSize: event.pageSize,
    }));
    this.loadOrders();
  }

  openOrderDetails(orderId: string): void {
    this.dialogService.open<OrderDetailsDialogComponent, OrderDetailsDialogData>(
      OrderDetailsDialogComponent,
      {
        minWidth: 'min(900px, 95vw)',
        maxWidth: '95vw',
        maxHeight: '90vh',
        data: {
          orderId,
        },
      },
    );
  }

  private loadOrders(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.ordersService.getOrders(this.query()).subscribe({
      next: (response) => {
        this.response.set(response);
        this.isLoading.set(false);
      },
      error: (error) => {
        this.errorMessage.set(this.getErrorMessage(error));
        this.response.set({
          orders: [],
          pagination: {
            currentPage: this.query().currentPage,
            pageSize: this.query().pageSize,
            totalPages: 0,
            totalOrders: 0,
          },
        });
        this.isLoading.set(false);
      },
    });
  }

  private getErrorMessage(error: unknown): string {
    if (!(error instanceof HttpErrorResponse)) {
      return 'Errore inatteso durante il caricamento ordini.';
    }

    if (error.status === 401) {
      return 'Sessione scaduta. Effettua nuovamente il login.';
    }

    if (error.status === 403) {
      return 'Non sei autorizzato a visualizzare gli ordini.';
    }

    if (error.status >= 500) {
      return 'Errore server durante il caricamento ordini.';
    }

    return 'Impossibile caricare gli ordini.';
  }
}
