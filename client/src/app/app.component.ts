import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { HeaderComponent } from './layout/header/header.component';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, HeaderComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss',
})
export class AppComponent implements OnInit {
  baseUrl = 'https://localhost:5001/api/';

  private http = inject(HttpClient);

  protected title = signal('Skinet');
  products: any[] = [];

  ngOnInit(): void {
    this.http.get<unknown>(this.baseUrl + 'products').subscribe({
      next: (response) => {
        console.log('products response', response);
        if (Array.isArray(response)) {
          this.products = response;
          return;
        }
        const data =
          (response as { data?: unknown; items?: unknown }).data ??
          (response as { items?: unknown }).items;
        this.products = Array.isArray(data) ? data : [];
        console.log('products count', this.products.length);
      },
      error: (error) => console.log(error),
      complete: () => console.log('complete'),
    });
  }
}
