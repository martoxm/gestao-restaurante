import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useRouter } from "next/navigation";
import { criarMovimentacaoEstoque } from "@/features/movimentacoes-estoque/services/movimentacoes-estoque";

export function useCriarMovimentacaoEstoque({ redirecionar = true } = {}) {
  const router = useRouter();
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: criarMovimentacaoEstoque,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["produtos"] });
      queryClient.invalidateQueries({ queryKey: ["movimentacoes-estoque"] });
      if (redirecionar) router.push("/movimentacoes");
    },
  });
}
