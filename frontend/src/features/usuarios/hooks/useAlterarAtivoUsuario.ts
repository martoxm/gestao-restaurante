import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { alterarAtivoUsuario } from "@/features/usuarios/services/usuarios";

export function useAlterarAtivoUsuario() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, ativo }: { id: string; ativo: boolean }) => alterarAtivoUsuario(id, ativo),
    onSuccess: (_, { ativo }) => {
      queryClient.invalidateQueries({ queryKey: ["usuarios"] });
      toast.success(ativo ? "Acesso reativado." : "Acesso desativado. A pessoa sai do sistema em até 15 minutos.");
    },
  });
}
