import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { MATERIAL_IMPORTS } from '../../../shared/material';

@Component({
  selector: 'app-forgot-password',
  imports: [CommonModule, ReactiveFormsModule, RouterLink, ...MATERIAL_IMPORTS],
  templateUrl: './forgot-password.component.html',
  styleUrl: './forgot-password.component.scss',
})
export class ForgotPasswordComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
  });

  isSubmitting = false;
  message: string | null = null;

  submit() {
    if (this.form.invalid || this.isSubmitting) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    this.message = null;

    this.authService.forgotPassword(this.form.getRawValue().email).subscribe({
      next: (response) => {
        this.message = response?.message ?? "Se l'email esiste, verrà inviato un link di reset.";
        this.isSubmitting = false;
      },
      error: () => {
        this.message = "Se l'email esiste, verrà inviato un link di reset.";
        this.isSubmitting = false;
      },
    });
  }

  getError(): string | null {
    const control = this.form.get('email');
    if (!control || !control.touched) {
      return null;
    }

    if (control.hasError('required')) {
      return 'Campo obbligatorio.';
    }

    if (control.hasError('email')) {
      return 'Email non valida.';
    }

    return null;
  }
}
