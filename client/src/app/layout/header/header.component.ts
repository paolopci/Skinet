import { Component } from '@angular/core';
import { MatBadge, MatButton, MatIcon } from '../../shared/material';

@Component({
  selector: 'app-header',
  imports: [MatIcon, MatButton, MatBadge],
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss',
})
export class HeaderComponent {}
