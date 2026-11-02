import { api } from "@/lib/axios";
import type { ResultadoPaginado } from "@/features/produtos/types/produto";
import type {
  CriarMovimentacaoEstoqueRequest,
  ListarMovimentacoesEstoqueParams,
  MovimentacaoEstoqueItemLista,
} from "@/features/movimentacoes-estoque/types/movimentacao-estoque";

export async function criarMovimentacaoEstoque(
  data: CriarMovimentacaoEstoqueRequest
): Promise<{ id: string }> {
  const response = await api.post<{ id: string }>("/movimentacoes-estoque", data);

  return response.data;
}

export async function listarMovimentacoesEstoque(
  params: ListarMovimentacoesEstoqueParams = {}
): Promise<ResultadoPaginado<MovimentacaoEstoqueItemLista>> {
  const response = await api.get<ResultadoPaginado<MovimentacaoEstoqueItemLista>>(
    "/movimentacoes-estoque",
    { params }
  );

  return response.data;
}
