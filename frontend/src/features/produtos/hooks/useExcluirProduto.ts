import { toast } from "sonner";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { excluirProduto } from "@/features/produtos/services/produtos";

export function useExcluirProduto() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: excluirProduto,
    onSuccess: () => {
      toast.success("Produto excluído");
      queryClient.invalidateQueries({ queryKey: ["produtos"] });
    },
  });
}
