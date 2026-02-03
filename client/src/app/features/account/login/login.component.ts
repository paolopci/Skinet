import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
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
}
