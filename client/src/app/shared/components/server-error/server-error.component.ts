import { Component } from '@angular/core';
import { Router, RouterLink } from '@angular/router';

@Component({
  selector: 'app-server-error',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './server-error.component.html',
  styleUrl: './server-error.component.scss',
})
export class ServerErrorComponent {
  error: Record<string, unknown> | null = null;
  stackTrace: string | null = null;
  statusCode: number | null = null;
  title: string | null = null;

  constructor(private router: Router) {
    const navigation = this.router.getCurrentNavigation();
    const errorState = navigation?.extras.state?.['error'] ?? history.state?.['error'];
    this.setError(errorState);
  }

  private setError(errorState: unknown): void {
    if (!errorState) {
      return;
    }

    if (typeof errorState === 'string') {
      this.title = errorState;
      return;
    }

    if (typeof errorState === 'object') {
      const payload = errorState as Record<string, unknown>;
      this.error = payload;

      const status = payload['status'];
      this.statusCode = typeof status === 'number' ? status : null;
      this.title = this.readString(payload, ['title', 'message', 'statusText']);
      this.stackTrace = this.readString(payload, ['details', 'detail', 'stackTrace', 'stack']);
    }
  }

  private readString(payload: Record<string, unknown>, keys: string[]): string | null {
    for (const key of keys) {
      const value = payload[key];
      if (typeof value === 'string' && value.trim().length > 0) {
        return value;
      }
    }
    return null;
  }
}
