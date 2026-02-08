import { Component, effect, inject, OnDestroy, signal } from '@angular/core';
import { MATERIAL_IMPORTS } from '../../shared/material';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { CartService } from '../../core/services/cart.service';
import { AuthStateService } from '../../core/services/auth-state.service';
import { AuthService } from '../../core/services/auth.service';
import { Router } from '@angular/router';
import { OrdersService } from '../../core/services/orders.service';
import { merge, of, Subscription, switchMap } from 'rxjs';

@Component({
  selector: 'app-header',
  imports: [...MATERIAL_IMPORTS, RouterLink, RouterLinkActive],
  standalone: true,
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss',
})
export class HeaderComponent implements OnDestroy {
  // Accesso ai signals del carrello (badge).
  cartService = inject(CartService);
  private readonly authState = inject(AuthStateService);
  private readonly authService = inject(AuthService);
  private readonly ordersService = inject(OrdersService);
  private readonly router = inject(Router);
  readonly isLoggedIn = this.authState.isAuthenticated;
  readonly user = this.authState.user;
  readonly exactMatch = { exact: true };
  readonly hasOrders = signal<boolean | null>(null);

  private hasOrdersSub: Subscription | null = null;

  constructor() {
    effect(() => {
      if (!this.isLoggedIn()) {
        this.hasOrdersSub?.unsubscribe();
        this.hasOrdersSub = null;
        this.hasOrders.set(null);
        return;
      }

      this.hasOrders.set(null);
      this.hasOrdersSub?.unsubscribe();
      this.hasOrdersSub = merge(of(void 0), this.ordersService.hasOrdersRefresh$)
        .pipe(switchMap(() => this.ordersService.hasOrders()))
        .subscribe({
          next: (value) => this.hasOrders.set(value),
          error: () => this.hasOrders.set(false),
        });
    });
  }

  logout() {
    this.authService.logout().subscribe({
      next: () => {
        this.ordersService.invalidateOrdersCache();
        this.hasOrders.set(null);
        this.router.navigateByUrl('/');
      },
      error: () => this.router.navigateByUrl('/'),
    });
  }

  ngOnDestroy(): void {
    this.hasOrdersSub?.unsubscribe();
    this.hasOrdersSub = null;
  }
}
