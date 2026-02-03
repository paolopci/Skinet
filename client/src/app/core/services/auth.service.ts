import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment.development';
import { AuthUser, LoginRequest, RefreshRequest, RegisterRequest } from '../../shared/models/auth';
import { AuthStateService } from './auth-state.service';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly baseUrl = environment.apiUrl;
  private readonly http = inject(HttpClient);
  private readonly authState = inject(AuthStateService);

  register(payload: RegisterRequest): Observable<AuthUser> {
    return this.http.post<AuthUser>(`${this.baseUrl}account/register`, payload).pipe(
      tap((user) => this.authState.setUser(user)),
    );
  }

  login(payload: LoginRequest): Observable<AuthUser> {
    return this.http.post<AuthUser>(`${this.baseUrl}account/login`, payload).pipe(
      tap((user) => this.authState.setUser(user)),
    );
  }

  refresh(payload: RefreshRequest): Observable<AuthUser> {
    return this.http.post<AuthUser>(`${this.baseUrl}account/refresh`, payload).pipe(
      tap((user) => this.authState.setUser(user)),
    );
  }

  logout(): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}account/logout`, {}).pipe(
      tap(() => this.authState.clear()),
    );
  }

  clearLocalSession() {
    this.authState.clear();
  }

  currentUser(): Observable<AuthUser> {
    return this.http.get<AuthUser>(`${this.baseUrl}account/current-user`).pipe(
      tap((user) => this.authState.setUser(user)),
    );
  }

  forgotPassword(email: string): Observable<{ message: string } | void> {
    return this.http.post<{ message: string }>(`${this.baseUrl}account/forgot-password`, {
      email,
    });
  }

  loadFromStorage() {
    this.authState.loadFromStorage();
  }
}
