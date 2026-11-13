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
  numero: number;
  status: StatusVenda;
  criadoEm: string;
  total: number;
  motivoCancelamento: MotivoCancelamento | null;
  detalheCancelamento: string | null;
  itens: ItemVenda[];
}

export interface VendaItemLista {
  id: string;
  numero: number;
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
  { gateway: "Falso", token: "tok_aprovado", rotulo: "Falso — aprovado" },
  { gateway: "Falso", token: "tok_saldo_insuficiente", rotulo: "Falso — recusado: saldo insuficiente" },
  { gateway: "Falso", token: "tok_fraude", rotulo: "Falso — recusado: suspeita de fraude" },
  { gateway: "Falso", token: "tok_gateway_fora", rotulo: "Falso — gateway fora do ar" },
  { gateway: "Stripe", token: "pm_card_visa", rotulo: "Stripe — Visa aprovado" },
  { gateway: "Stripe", token: "pm_card_mastercard", rotulo: "Stripe — Mastercard aprovado" },
  { gateway: "Stripe", token: "pm_card_chargeDeclined", rotulo: "Stripe — recusado (genérico)" },
  { gateway: "Stripe", token: "pm_card_chargeDeclinedInsufficientFunds", rotulo: "Stripe — recusado: saldo insuficiente" },
];

const gatewayLigado = process.env.NEXT_PUBLIC_GATEWAY_PAGAMENTO;

export const CARTOES_DISPONIVEIS = gatewayLigado
  ? CARTOES_TESTE.filter((cartao) => cartao.gateway.toLowerCase() === gatewayLigado.toLowerCase())
  : CARTOES_TESTE;

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

export function codigoVenda(numero: number) {
  return `#${numero}`;
}
