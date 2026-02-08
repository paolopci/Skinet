export type OrdersSortBy = 'dataordine' | 'orderid';
export type OrdersSortOrder = 'asc' | 'desc';

export type OrdersQueryParams = {
  sortBy: OrdersSortBy;
  order: OrdersSortOrder;
  quarter: number | null;
  year: number | null;
  search: string;
  currentPage: number;
  pageSize: number;
};
