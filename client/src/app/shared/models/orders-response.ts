import { OrderListItem } from './order-list-item';
import { OrdersPagination } from './orders-pagination';

export type OrdersResponse = {
  orders: OrderListItem[];
  pagination: OrdersPagination;
};
