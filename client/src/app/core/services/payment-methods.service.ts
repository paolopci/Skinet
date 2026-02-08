import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment.development';
import { SavedPaymentMethod } from '../../shared/models/saved-payment-method';

@Injectable({
  providedIn: 'root',
})
export class PaymentMethodsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  getSavedPaymentMethods() {
    return this.http.get<SavedPaymentMethod[]>(`${this.baseUrl}payments/payment-methods`);
  }

  setDefaultPaymentMethod(paymentMethodId: string) {
    return this.http.post<{ isSuccess: boolean }>(
      `${this.baseUrl}payments/payment-methods/${paymentMethodId}/default`,
      {},
    );
  }

  deletePaymentMethod(paymentMethodId: string) {
    return this.http.delete<void>(`${this.baseUrl}payments/payment-methods/${paymentMethodId}`);
  }
}
