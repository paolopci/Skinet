import { Component, inject } from '@angular/core';
import { MATERIAL_IMPORTS } from '../../shared/material';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { CartService } from '../../core/services/cart.service';

@Component({
  selector: 'app-header',
  imports: [...MATERIAL_IMPORTS, RouterLink, RouterLinkActive],
  standalone: true,
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss',
})
export class HeaderComponent {
  // Accesso ai signals del carrello (badge).
  cartService = inject(CartService);
  isLoggedIn = false;
  readonly exactMatch = { exact: true };
}
