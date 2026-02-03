import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { MATERIAL_IMPORTS } from '../../../shared/material';
import { extractValidationErrorMap } from '../../../shared/utils/api-error';

@Component({
  selector: 'app-register',
  imports: [CommonModule, ReactiveFormsModule, RouterLink, ...MATERIAL_IMPORTS],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss',
})
export class RegisterComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly form = this.fb.nonNullable.group({
    firstName: ['', [Validators.required]],
    lastName: ['', [Validators.required]],
    email: ['', [Validators.required, Validators.email]],
    phoneNumber: ['', [Validators.required]],
    password: ['', [Validators.required, Validators.minLength(8)]],
    confirmPassword: ['', [Validators.required]],
    address: this.fb.nonNullable.group({
      street: ['', [Validators.required]],
      city: ['', [Validators.required]],
      state: ['', [Validators.required]],
      postalCode: ['', [Validators.required]],
    }),
  });

  validationErrors: Record<string, string[]> | null = null;
  isSubmitting = false;

  submit() {
    if (this.form.invalid || this.isSubmitting) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    this.validationErrors = null;

    this.authService.register(this.form.getRawValue()).subscribe({
      next: () => {
        this.router.navigateByUrl('/login');
        this.isSubmitting = false;
      },
      error: (error) => {
        this.validationErrors = extractValidationErrorMap(error?.error) ?? null;
        this.isSubmitting = false;
      },
    });
  }

  getError(controlName: 'firstName' | 'lastName' | 'email' | 'phoneNumber' | 'password' | 'confirmPassword'): string | null {
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

    if (controlName === 'password' && control.hasError('minlength')) {
      return 'La password deve avere almeno 8 caratteri.';
    }

    return null;
  }

  getAddressError(field: 'street' | 'city' | 'state' | 'postalCode'): string | null {
    const control = this.form.get('address')?.get(field);
    if (!control || !control.touched) {
      return null;
    }

    if (control.hasError('required')) {
      return 'Campo obbligatorio.';
    }

    return null;
  }
}
