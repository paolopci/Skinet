import { Component, computed, inject, signal } from '@angular/core';
import { AsyncPipe } from '@angular/common';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { HeaderComponent } from './layout/header/header.component';
import { LoadingService } from './core/services/loading.service';
import { MatProgressBar } from '@angular/material/progress-bar';
import { filter } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CartService } from './core/services/cart.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, HeaderComponent, AsyncPipe, MatProgressBar],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss',
})
export class AppComponent {
  protected title = signal('Skinet');
  private readonly loadingService = inject(LoadingService);
  private readonly router = inject(Router);
  private readonly cartService = inject(CartService);
  readonly loading$ = this.loadingService.loading$;
  private readonly currentUrl = signal(this.router.url);
  readonly isShopRoute = computed(() => this.currentUrl().startsWith('/shop'));

  constructor() {
    this.cartService.loadCart();
    this.router.events
      .pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd),
        takeUntilDestroyed(),
      )
      .subscribe((event) => {
        this.currentUrl.set(event.urlAfterRedirects);
      });
  }
}
