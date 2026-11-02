import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { alterarRoleUsuario } from "@/features/usuarios/services/usuarios";
import type { RoleUsuario } from "@/features/usuarios/types/usuario";

export function useAlterarRoleUsuario() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, role }: { id: string; role: RoleUsuario }) => alterarRoleUsuario(id, role),
    onSuccess: (_, { role }) => {
      queryClient.invalidateQueries({ queryKey: ["usuarios"] });
      toast.success(
        `Agora é ${role}. A pessoa vê a mudança em até 15 minutos, ou ao entrar de novo.`
      );
    },
  });
}
