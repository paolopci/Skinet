import { ChangeDetectorRef, Component, inject } from '@angular/core';
import { JsonPipe, NgFor, NgIf } from '@angular/common';
import { MATERIAL_IMPORTS } from '../../shared/material';
import { HttpClient } from '@angular/common/http';
import { extractValidationErrors } from '../../shared/utils/api-error';

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
  private cdr = inject(ChangeDetectorRef);
  lastError: unknown | null = null;
  validationErrors: string[] = [];
  validationProblemDetails: unknown | null = null;
  showValidationDetails = false;
  validationHasResponse = false;

  private handleError(error: unknown): void {
    this.lastError = error;
    console.error('Errore API', error);
  }

  get400ValidationError(): void {
    this.validationHasResponse = false;
    this.http.post(`${this.baseUrl}buggy/validationerror`, {}).subscribe({
      next: () => {
        this.validationErrors = [];
        this.validationProblemDetails = null;
        this.showValidationDetails = false;
        this.validationHasResponse = false;
      },
      error: (error) => {
        this.validationErrors = extractValidationErrors((error as { error?: unknown })?.error);
        this.validationProblemDetails = (error as { error?: unknown })?.error ?? null;
        this.showValidationDetails = false;
        this.validationHasResponse = true;
        this.handleError(error);
        this.cdr.detectChanges();
      },
    });
  }

  get401Error(): void {
    this.http.get(`${this.baseUrl}buggy/unauthorized`).subscribe({
      error: (error) => {
        this.handleError(error);
        this.cdr.detectChanges();
      },
    });
  }

  get403Error(): void {
    this.http.get(`${this.baseUrl}buggy/forbidden`).subscribe({
      error: (error) => {
        this.handleError(error);
        this.cdr.detectChanges();
      },
    });
  }

  get404Error(): void {
    this.http.get(`${this.baseUrl}buggy/not-found`).subscribe({
      error: (error) => {
        this.handleError(error);
        this.cdr.detectChanges();
      },
    });
  }

  get500Error(): void {
    this.http.get(`${this.baseUrl}buggy/server-error`).subscribe({
      error: (error) => {
        this.handleError(error);
        this.cdr.detectChanges();
      },
    });
  }

  get503Error(): void {
    this.http.get(`${this.baseUrl}buggy/service-unavailable`).subscribe({
      error: (error) => {
        this.handleError(error);
        this.cdr.detectChanges();
      },
    });
  }

  get550Error(): void {
    this.http.get(`${this.baseUrl}buggy/status-550`).subscribe({
      error: (error) => {
        this.handleError(error);
        this.cdr.detectChanges();
      },
    });
  }

  get505Error(): void {
    this.http.get(`${this.baseUrl}buggy/status-505`).subscribe({
      error: (error) => {
        this.handleError(error);
        this.cdr.detectChanges();
      },
    });
  }

  getRedirectError(): void {
    this.http.get(`${this.baseUrl}buggy/redirect-products`).subscribe({
      error: (error) => {
        this.handleError(error);
        this.cdr.detectChanges();
      },
    });
  }
}
