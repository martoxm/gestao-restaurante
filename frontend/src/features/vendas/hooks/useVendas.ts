import { useQuery } from "@tanstack/react-query";
import { listarVendas } from "@/features/vendas/services/vendas";
import { estaEmAndamento } from "@/features/vendas/types/venda";

export function useVendas() {
  return useQuery({
    queryKey: ["vendas"],
    queryFn: listarVendas,
    refetchInterval: (query) =>
      query.state.data?.itens.some((venda) => estaEmAndamento(venda.status)) ? 2000 : false,
  });
}
