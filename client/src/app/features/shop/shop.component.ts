import { Component, inject, signal } from '@angular/core';
import { ShopService } from '../../core/services/shop.service';
import { Product } from '../../shared/models/product';
import { ProductItemComponent } from './product-item/product-item.component';
import { MatDialog } from '@angular/material/dialog';
import { MATERIAL_IMPORTS } from '../../shared/material';
import { MatSelectionListChange } from '@angular/material/list';
import {
  FiltersDialogComponent,
  FiltersDialogData,
} from './filters-dialog/filters-dialog.component';

@Component({
  selector: 'app-shop',
  imports: [ProductItemComponent, ...MATERIAL_IMPORTS],
  standalone: true,
  templateUrl: './shop.component.html',
  styleUrl: './shop.component.scss',
})
export class ShopComponent {
  private shopService = inject(ShopService);
  private dialogService = inject(MatDialog);
  private allProducts = signal<Product[]>([]);
  products = signal<Product[]>([]);

  selectedBrands: string[] = [];
  selectedTypes: string[] = [];
  selectedSort = 'name';
  sortOptions = [
    { name: 'Nome A-Z', value: 'name' },
    { name: 'Prezzo \u2191', value: 'priceAsc' },
    { name: 'Prezzo \u2193', value: 'priceDesc' },
  ];

  get selectedSortLabel() {
    return this.sortOptions.find((option) => option.value === this.selectedSort)?.name ?? 'Sort';
  }

  get selectedSortIcon() {
    if (this.selectedSort === 'name') return 'sort_by_alpha';
    if (this.selectedSort === 'priceAsc') return 'arrow_upward';
    return 'arrow_downward';
  }

  ngOnInit(): void {
    this.initializeShop();
  }

  initializeShop() {
    this.shopService.getBrands();
    this.shopService.getTypes();

    this.shopService.getProducts().subscribe({
      next: (response) => {
        this.allProducts.set(response);
        this.applyFilters();
      },
      error: (error) => {
        console.log(error);
      },
      complete: () => {
        console.log('Request has completed');
      },
    });
  }

  openFiltersDialog() {
    this.dialogService
      .open<
        FiltersDialogComponent,
        FiltersDialogData,
        {
          selectedBrands: string[];
          selectedTypes: string[];
        }
      >(FiltersDialogComponent, {
        minWidth: '500px',
        data: {
          brands: this.shopService.brands,
          types: this.shopService.types,
          selectedBrands: this.selectedBrands,
          selectedTypes: this.selectedTypes,
        },
      })
      .afterClosed()
      .subscribe((result) => {
        if (!result) return;
        this.selectedBrands = result.selectedBrands;
        this.selectedTypes = result.selectedTypes;
        this.applyFilters();
      });
  }

  onSortChange(event: MatSelectionListChange) {
    const selectedOption = event.options[0];
    if (!selectedOption) return;
    this.selectedSort = selectedOption.value as string;
    this.applyFilters();
  }

  private applyFilters() {
    const selectedBrands = this.selectedBrands;
    const selectedTypes = this.selectedTypes;
    const filtered = this.allProducts().filter((product) => {
      const brandMatch = selectedBrands.length === 0 || selectedBrands.includes(product.brand);
      const typeMatch = selectedTypes.length === 0 || selectedTypes.includes(product.type);
      return brandMatch && typeMatch;
    });

    const sorted = [...filtered].sort((left, right) => {
      if (this.selectedSort === 'priceAsc') {
        return left.price - right.price || left.id - right.id;
      }
      if (this.selectedSort === 'priceDesc') {
        return right.price - left.price || left.id - right.id;
      }

      const nameCompare = left.name.localeCompare(right.name, undefined, {
        sensitivity: 'base',
      });
      return nameCompare || left.id - right.id;
    });

    this.products.set(sorted);
  }
}
