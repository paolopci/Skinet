import { computed, Injectable, signal } from '@angular/core';
import { AuthUser } from '../../shared/models/auth';

@Injectable({
  providedIn: 'root',
})
export class AuthStateService {
  private readonly storageKey = 'auth_user';
  private readonly userSignal = signal<AuthUser | null>(null);

  user = this.userSignal.asReadonly();
  isAuthenticated = computed(() => !!this.userSignal()?.token);

  loadFromStorage() {
    const raw = localStorage.getItem(this.storageKey);
    if (!raw) {
      return;
    }

    try {
      const parsed = JSON.parse(raw) as AuthUser;
      if (parsed?.token) {
        this.userSignal.set(parsed);
      } else {
        this.clear();
      }
    } catch {
      this.clear();
    }
  }

  setUser(user: AuthUser) {
    this.userSignal.set(user);
    localStorage.setItem(this.storageKey, JSON.stringify(user));
  }

  clear() {
    this.userSignal.set(null);
    localStorage.removeItem(this.storageKey);
  }
}
