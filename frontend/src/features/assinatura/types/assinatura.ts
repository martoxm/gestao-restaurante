export type PlanoPago = "Basico" | "Pro";

export interface PlanoAVenda {
  plano: PlanoPago;
  nome: string;
  preco: number;
  dias: number;
}

export interface CheckoutAssinaturaResponse {
  cobrancaId: string;
  urlPagamento: string;
}
