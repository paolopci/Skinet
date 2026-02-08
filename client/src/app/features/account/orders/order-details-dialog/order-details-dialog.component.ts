import { DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MATERIAL_IMPORTS } from '../../../../shared/material';
import { OrdersService } from '../../../../core/services/orders.service';
import { OrderDetailsResponse } from '../../../../shared/models/order-details-response';

export type OrderDetailsDialogData = {
  orderId: string;
};

@Component({
  selector: 'app-order-details-dialog',
  standalone: true,
  imports: [DecimalPipe, ...MATERIAL_IMPORTS],
  templateUrl: './order-details-dialog.component.html',
  styleUrl: './order-details-dialog.component.scss',
})
export class OrderDetailsDialogComponent implements OnInit {
  private readonly ordersService = inject(OrdersService);
  private readonly dialogRef = inject(MatDialogRef<OrderDetailsDialogComponent>);
  readonly data = inject<OrderDetailsDialogData>(MAT_DIALOG_DATA);

  readonly details = signal<OrderDetailsResponse | null>(null);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.loadOrderDetails();
  }

  retry(): void {
    this.loadOrderDetails();
  }

  close(): void {
    this.dialogRef.close();
  }

  private loadOrderDetails(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.ordersService.getOrderDetails(this.data.orderId).subscribe({
      next: (response) => {
        this.details.set(response);
        this.isLoading.set(false);
      },
      error: (error) => {
        this.errorMessage.set(this.getErrorMessage(error));
        this.details.set(null);
        this.isLoading.set(false);
      },
    });
  }

  private getErrorMessage(error: unknown): string {
    if (!(error instanceof HttpErrorResponse)) {
      return 'Errore inatteso durante il caricamento dettaglio ordine.';
    }

    if (error.status === 404) {
      return 'Ordine non trovato o non più disponibile.';
    }

    if (error.status === 403) {
      return 'Non sei autorizzato a visualizzare questo ordine.';
    }

    if (error.status === 401) {
      return 'Sessione scaduta. Effettua di nuovo il login.';
    }

    return 'Impossibile caricare il dettaglio ordine. Riprova.';
  }
}
