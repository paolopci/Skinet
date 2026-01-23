import { Component, inject, signal } from '@angular/core';
import { ShopService } from '../../core/services/shop.service';
import { Product } from '../../shared/models/product';
import { MatCard } from '../../shared/material';

@Component({
  selector: 'app-shop',
  imports: [MatCard],
  standalone: true,
  templateUrl: './shop.component.html',
  styleUrl: './shop.component.scss',
})
export class ShopComponent {
  private shopService = inject(ShopService);
  //protected title = signal('Skinet');
  products = signal<Product[]>([]);

  ngOnInit(): void {
    this.shopService.getProducts().subscribe({
      next: (response) => {
        this.products.set(response);
      },
      error: (error) => {
        console.log(error);
      },
      complete: () => {
        console.log('Request has completed');
      },
    });
  }
}
