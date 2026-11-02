export type TipoMovimentacaoEstoque = "Entrada" | "Saida";

export interface CriarMovimentacaoEstoqueRequest {
  produtoId: string;
  tipo: TipoMovimentacaoEstoque;
  quantidade: number;
  observacoes?: string;
}

export interface MovimentacaoEstoqueItemLista {
  id: string;
  produtoId: string;
  nomeProduto: string;
  tipo: TipoMovimentacaoEstoque;
  quantidade: number;
  dataMovimentacao: string;
  observacoes: string | null;
}

export interface ListarMovimentacoesEstoqueParams {
  pagina?: number;
  tamanhoPagina?: number;
  produtoId?: string;
  tipo?: TipoMovimentacaoEstoque;
  dataInicio?: string;
  dataFim?: string;
}
