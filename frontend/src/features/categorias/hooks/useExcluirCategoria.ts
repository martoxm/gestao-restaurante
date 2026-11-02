import { toast } from "sonner";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { excluirCategoria } from "@/features/categorias/services/categorias";

export function useExcluirCategoria() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: excluirCategoria,
    onSuccess: () => {
      toast.success("Categoria excluída");
      queryClient.invalidateQueries({ queryKey: ["categorias"] });
    },
  });
}
