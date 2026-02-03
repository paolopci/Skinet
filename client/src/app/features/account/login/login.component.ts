import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { SnackbarService } from '../../../core/services/snackbar.service';
import { MATERIAL_IMPORTS } from '../../../shared/material';
import { extractValidationErrorMap } from '../../../shared/utils/api-error';

@Component({
  selector: 'app-login',
  imports: [CommonModule, ReactiveFormsModule, RouterLink, ...MATERIAL_IMPORTS],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly snackbar = inject(SnackbarService);

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
  });

  validationErrors: Record<string, string[]> | null = null;
  isSubmitting = false;
  showPassword = false;

  togglePasswordVisibility() {
    this.showPassword = !this.showPassword;
  }

  submit() {
    if (this.form.invalid || this.isSubmitting) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    this.validationErrors = null;

    this.authService.login(this.form.getRawValue()).subscribe({
      next: () => {
        const returnUrl = this.router.routerState.snapshot.root.queryParams['returnUrl'];
        this.router.navigateByUrl(returnUrl ?? '/');
        this.isSubmitting = false;
      },
      error: (error) => {
        this.validationErrors = extractValidationErrorMap(error?.error) ?? null;
        if (this.validationErrors) {
          this.snackbar.showWarning('Ci sono errori di validazione.');
        }
        this.snackbar.showError(this.getAuthErrorMessage(error) ?? 'Si è verificato un errore.');
        this.isSubmitting = false;
      },
    });
  }

  getError(controlName: 'email' | 'password'): string | null {
    const control = this.form.get(controlName);
    if (!control || !control.touched) {
      return null;
    }

    if (control.hasError('required')) {
      return 'Campo obbligatorio.';
    }

    if (controlName === 'email' && control.hasError('email')) {
      return 'Email non valida.';
    }

    return null;
  }

  private getAuthErrorMessage(error: any): string | null {
    // Logghiamo l'errore completo per debug se il problema persiste
    console.error('Dettagli errore login:', error);

    const status = error?.status;
    const errorData = error?.error;

    // 1. Controllo prioritario sullo stato HTTP
    if (status == 401) {
      return 'Email o password non corretti.';
    }

    // 2. Analisi approfondita del corpo della risposta
    let message = '';
    let extractedStatus: any = null;

    if (errorData) {
      if (typeof errorData === 'object') {
        // Supportiamo sia camelCase che PascalCase (comune in .NET)
        message = (errorData.message || errorData.Message || errorData.title || errorData.Title || '').toLowerCase();
        extractedStatus = errorData.statusCode || errorData.StatusCode || errorData.status || errorData.Status;
      } else if (typeof errorData === 'string') {
        message = errorData.toLowerCase();
      }
    }

    // Se il corpo indica un 401 o contiene parole chiave sull'autenticazione
    if (extractedStatus == 401 ||
      message.includes('credenziali') ||
      message.includes('password') ||
      message.includes('unauthorized') ||
      message.includes('autorizzato') ||
      message.includes('invalid')) {
      return 'Email o password non corretti.';
    }

    // 3. Fallback per status 0 (solo se non abbiamo trovato indizi di 401 sopra)
    if (status === 0 || !status) {
      return 'Server non raggiungibile. Riprova più tardi.';
    }

    // 4. Messaggio di fallback finale
    const finalMessage = (typeof errorData === 'object' && (errorData.message || errorData.Message))
      ? (errorData.message || errorData.Message)
      : 'Si è verificato un errore durante l\'accesso.';

    return finalMessage;
  }
}
