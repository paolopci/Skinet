import { Component, inject } from '@angular/core';
import { JsonPipe, NgFor, NgIf } from '@angular/common';
import { MATERIAL_IMPORTS } from '../../shared/material';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-test-error',
  standalone: true,
  imports: [JsonPipe, NgFor, NgIf, ...MATERIAL_IMPORTS],
  templateUrl: './test-error.component.html',
  styleUrl: './test-error.component.scss',
})
export class TestErrorComponent {
  baseUrl = 'https://localhost:5001/api/';
  private http = inject(HttpClient);
  lastError: unknown | null = null;
  validationErrors: string[] = [];

  private handleError(error: unknown): void {
    this.lastError = error;
    console.error('Errore API', error);
  }

  get400ValidationError(): void {
    this.http.post(`${this.baseUrl}buggy/validationerror`, {}).subscribe({
      next: () => {
        this.validationErrors = [];
      },
      error: (error) => {
        const errorMap = (error as { error?: { errors?: Record<string, string[]> } })?.error
          ?.errors;
        this.validationErrors = errorMap ? Object.values(errorMap).flat() : [];
        this.handleError(error);
      },
    });
  }

  get401Error(): void {
    this.http.get(`${this.baseUrl}buggy/unauthorized`).subscribe({
      error: (error) => this.handleError(error),
    });
  }

  get403Error(): void {
    this.http.get(`${this.baseUrl}buggy/forbidden`).subscribe({
      error: (error) => this.handleError(error),
    });
  }

  get404Error(): void {
    this.http.get(`${this.baseUrl}buggy/not-found`).subscribe({
      error: (error) => this.handleError(error),
    });
  }

  get500Error(): void {
    this.http.get(`${this.baseUrl}buggy/server-error`).subscribe({
      error: (error) => this.handleError(error),
    });
  }

  get503Error(): void {
    this.http.get(`${this.baseUrl}buggy/service-unavailable`).subscribe({
      error: (error) => this.handleError(error),
    });
  }

  get550Error(): void {
    this.http.get(`${this.baseUrl}buggy/status-550`).subscribe({
      error: (error) => this.handleError(error),
    });
  }

  get505Error(): void {
    this.http.get(`${this.baseUrl}buggy/status-505`).subscribe({
      error: (error) => this.handleError(error),
    });
  }

  getRedirectError(): void {
    this.http.get(`${this.baseUrl}buggy/redirect-products`).subscribe({
      error: (error) => this.handleError(error),
    });
  }
}
