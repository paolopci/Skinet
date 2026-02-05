import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment.development';
import { AddressRequest } from '../../shared/models/auth';

@Injectable({
  providedIn: 'root',
})
export class AccountService {
  private readonly baseUrl = environment.apiUrl;
  private readonly http = inject(HttpClient);

  getAddress(): Observable<AddressRequest> {
    return this.http.get<AddressRequest>(`${this.baseUrl}account/address`);
  }

  updateAddress(payload: AddressRequest): Observable<AddressRequest> {
    return this.http.put<AddressRequest>(`${this.baseUrl}account/address`, payload);
  }
}
