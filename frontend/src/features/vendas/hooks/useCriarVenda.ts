import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { criarVenda } from "@/features/vendas/services/vendas";

export function useCriarVenda() {
  const router = useRouter();
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: criarVenda,
    onSuccess: ({ id }) => {
      queryClient.invalidateQueries({ queryKey: ["vendas"] });
      queryClient.invalidateQueries({ queryKey: ["produtos"] });
      toast.info("Venda registrada", { description: "Reservando o estoque e cobrando o cartão..." });
      router.push(`/vendas/${id}`);
    },
  });
}
