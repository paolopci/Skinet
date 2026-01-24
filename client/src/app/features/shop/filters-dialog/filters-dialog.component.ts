import { Component, inject } from '@angular/core';
import { ShopService } from '../../../core/services/shop.service';
import { MATERIAL_IMPORTS } from '../../../shared/material';

@Component({
  selector: 'app-filters-dialog',
  imports: [...MATERIAL_IMPORTS],
  standalone: true,
  templateUrl: './filters-dialog.component.html',
  styleUrl: './filters-dialog.component.scss',
})
export class FiltersDialogComponent {
  shopService = inject(ShopService);
}
