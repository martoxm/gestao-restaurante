export type StatusVenda = "Pendente" | "AguardandoPagamento" | "Confirmada" | "Cancelada";

export type MotivoCancelamento = "EstoqueInsuficiente" | "PagamentoRecusado" | "SemPagamento";

export interface ItemVendaRequest {
  produtoId: string;
  quantidade: number;
}

export interface CriarVendaRequest {
  itens: ItemVendaRequest[];
  tokenCartao: string;
}

export interface ItemVenda {
  produtoId: string;
  nomeProduto: string;
  precoUnitario: number;
  quantidade: number;
  subtotal: number;
}

export interface VendaDetalhes {
  id: string;
  status: StatusVenda;
  criadoEm: string;
  total: number;
  motivoCancelamento: MotivoCancelamento | null;
  detalheCancelamento: string | null;
  itens: ItemVenda[];
}

export interface VendaItemLista {
  id: string;
  status: StatusVenda;
  criadoEm: string;
  quantidadeItens: number;
  total: number;
  motivoCancelamento: MotivoCancelamento | null;
}

export function estaEmAndamento(status: StatusVenda) {
  return status === "Pendente" || status === "AguardandoPagamento";
}

export const ROTULO_STATUS: Record<StatusVenda, string> = {
  Pendente: "Pendente",
  AguardandoPagamento: "Aguardando pagamento",
  Confirmada: "Confirmada",
  Cancelada: "Cancelada",
};

export const CARTOES_TESTE = [
  { token: "tok_aprovado", rotulo: "Falso — aprovado" },
  { token: "tok_saldo_insuficiente", rotulo: "Falso — recusado: saldo insuficiente" },
  { token: "tok_fraude", rotulo: "Falso — recusado: suspeita de fraude" },
  { token: "tok_gateway_fora", rotulo: "Falso — gateway fora do ar" },
  { token: "pm_card_visa", rotulo: "Stripe — Visa aprovado" },
  { token: "pm_card_mastercard", rotulo: "Stripe — Mastercard aprovado" },
  { token: "pm_card_chargeDeclined", rotulo: "Stripe — recusado (genérico)" },
  { token: "pm_card_chargeDeclinedInsufficientFunds", rotulo: "Stripe — recusado: saldo insuficiente" },
];

export function formatarMoeda(valor: number) {
  return valor.toLocaleString("pt-BR", { style: "currency", currency: "BRL" });
}

export interface ExplicacaoCancelamento {
  titulo: string;
  rotuloCurto: string;
  descricao: string;
  oQueFazer: string;
}

export function explicarCancelamento(
  motivo: MotivoCancelamento | null,
  detalhe?: string | null
): ExplicacaoCancelamento {
  switch (motivo) {
    case "EstoqueInsuficiente":
      return {
        titulo: "Sem estoque suficiente",
        rotuloCurto: "Sem estoque",
        descricao:
          detalhe ??
          "Pelo menos um item não tinha quantidade suficiente em estoque. Nada foi baixado e o cliente não foi cobrado.",
        oQueFazer:
          "Confira o estoque dos produtos, registre uma entrada se a mercadoria chegou, ou refaça a venda sem o item em falta.",
      };
    case "PagamentoRecusado":
      return {
        titulo: "Pagamento recusado",
        rotuloCurto: "Pagamento recusado",
        descricao: detalhe ?? "A operadora do cartão recusou a cobrança.",
        oQueFazer:
          "O cliente não foi cobrado e o estoque já foi devolvido. Peça outra forma de pagamento e registre a venda de novo.",
      };
    case "SemPagamento":
      return {
        titulo: "Venda sem forma de pagamento",
        rotuloCurto: "Sem pagamento",
        descricao: detalhe ?? "A venda não tinha cartão para cobrar.",
        oQueFazer: "O estoque foi devolvido. Registre a venda de novo informando o pagamento.",
      };
    default:
      return {
        titulo: "Venda cancelada",
        rotuloCurto: "Cancelada",
        descricao: "Essa venda foi cancelada antes de o motivo passar a ser registrado.",
        oQueFazer: "Se o estoque chegou a ser baixado, ele já foi devolvido.",
      };
  }
}

export function formatarDataHora(data: string) {
  return new Date(data).toLocaleString("pt-BR", { dateStyle: "short", timeStyle: "short" });
}

export function codigoVenda(id: string) {
  return `#${id.slice(0, 6).toUpperCase()}`;
}
