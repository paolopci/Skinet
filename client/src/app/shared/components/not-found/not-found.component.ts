import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MATERIAL_IMPORTS } from '../../material';

@Component({
  selector: 'app-not-found',
  standalone: true,
  imports: [...MATERIAL_IMPORTS, RouterLink],
  templateUrl: './not-found.component.html',
  styleUrl: './not-found.component.scss',
})
export class NotFoundComponent {}
