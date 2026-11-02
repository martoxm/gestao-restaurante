import { useQuery } from "@tanstack/react-query";
import { obterVendaPorId } from "@/features/vendas/services/vendas";
import { estaEmAndamento } from "@/features/vendas/types/venda";

export function useVenda(id: string) {
  return useQuery({
    queryKey: ["vendas", id],
    queryFn: () => obterVendaPorId(id),
    enabled: !!id,
    refetchInterval: (query) =>
      query.state.data && estaEmAndamento(query.state.data.status) ? 2000 : false,
  });
}
