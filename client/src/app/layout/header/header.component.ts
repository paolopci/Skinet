import { Component } from '@angular/core';
import { MATERIAL_IMPORTS } from '../../shared/material';
import { RouterLink, RouterLinkActive } from '@angular/router';

@Component({
  selector: 'app-header',
  imports: [...MATERIAL_IMPORTS, RouterLink, RouterLinkActive],
  standalone: true,
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss',
})
export class HeaderComponent {
  isLoggedIn = false;
  readonly exactMatch = { exact: true };
}
