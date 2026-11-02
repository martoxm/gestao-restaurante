import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { listarMovimentacoesEstoque } from "@/features/movimentacoes-estoque/services/movimentacoes-estoque";
import type { ListarMovimentacoesEstoqueParams } from "@/features/movimentacoes-estoque/types/movimentacao-estoque";

export function useMovimentacoesEstoque(params: ListarMovimentacoesEstoqueParams) {
  return useQuery({
    queryKey: ["movimentacoes-estoque", params],
    queryFn: () => listarMovimentacoesEstoque(params),
    placeholderData: keepPreviousData,
  });
}
