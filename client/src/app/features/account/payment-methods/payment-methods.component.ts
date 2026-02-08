import { Component, inject, OnInit } from '@angular/core';
import { MATERIAL_IMPORTS } from '../../../shared/material';
import { PaymentMethodsService } from '../../../core/services/payment-methods.service';
import { SavedPaymentMethod } from '../../../shared/models/saved-payment-method';
import { firstValueFrom } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { SnackbarService } from '../../../core/services/snackbar.service';

@Component({
  selector: 'app-payment-methods',
  standalone: true,
  imports: [...MATERIAL_IMPORTS],
  templateUrl: './payment-methods.component.html',
  styleUrl: './payment-methods.component.scss',
})
export class PaymentMethodsComponent implements OnInit {
  private readonly paymentMethodsService = inject(PaymentMethodsService);
  private readonly snackbar = inject(SnackbarService);

  methods: SavedPaymentMethod[] = [];
  isLoading = false;
  isSaving = false;

  async ngOnInit(): Promise<void> {
    await this.loadMethods();
  }

  async loadMethods(): Promise<void> {
    this.isLoading = true;
    try {
      this.methods = await firstValueFrom(this.paymentMethodsService.getSavedPaymentMethods());
    } catch (error) {
      this.handleApiError(error, 'Impossibile caricare le carte salvate.');
    } finally {
      this.isLoading = false;
    }
  }

  async setDefault(paymentMethodId: string): Promise<void> {
    if (this.isSaving) {
      return;
    }

    this.isSaving = true;
    try {
      await firstValueFrom(this.paymentMethodsService.setDefaultPaymentMethod(paymentMethodId));
      this.methods = this.methods.map((method) => ({
        ...method,
        isDefault: method.id === paymentMethodId,
      }));
      this.snackbar.showInfo('Metodo predefinito aggiornato.');
    } catch (error) {
      this.handleApiError(error, 'Impossibile impostare il metodo predefinito.');
    } finally {
      this.isSaving = false;
    }
  }

  async deleteMethod(paymentMethodId: string): Promise<void> {
    if (this.isSaving) {
      return;
    }

    this.isSaving = true;
    try {
      await firstValueFrom(this.paymentMethodsService.deletePaymentMethod(paymentMethodId));
      this.methods = this.methods.filter((method) => method.id !== paymentMethodId);
      this.snackbar.showInfo('Carta eliminata con successo.');
    } catch (error) {
      this.handleApiError(error, 'Impossibile eliminare la carta.');
    } finally {
      this.isSaving = false;
    }
  }

  private handleApiError(error: unknown, fallbackMessage: string): void {
    if (!(error instanceof HttpErrorResponse)) {
      this.snackbar.showError(fallbackMessage);
      return;
    }

    if (error.status === 401) {
      this.snackbar.showWarning('Sessione scaduta. Effettua di nuovo l’accesso.');
      return;
    }

    if (error.status === 403) {
      this.snackbar.showWarning('Operazione non autorizzata sul metodo richiesto.');
      return;
    }

    if (error.status === 404) {
      this.snackbar.showWarning('Metodo di pagamento non trovato.');
      return;
    }

    if (error.status === 502) {
      this.snackbar.showWarning('Provider pagamenti non disponibile. Riprova più tardi.');
      return;
    }

    this.snackbar.showError(fallbackMessage);
  }
}
