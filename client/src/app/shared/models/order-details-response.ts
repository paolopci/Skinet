import { OrderDetailItem } from './order-detail-item';

export type OrderDetailsResponse = {
  orderId: string;
  userId: string;
  data: string;
  tipoPagamento: string;
  numeroCarta: string | null;
  importo: number;
  stato: string;
  dettagli: OrderDetailItem[];
};
