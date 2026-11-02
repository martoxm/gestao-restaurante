import { useQuery } from "@tanstack/react-query";
import { listarUsuarios } from "@/features/usuarios/services/usuarios";

export function useUsuarios() {
  return useQuery({
    queryKey: ["usuarios"],
    queryFn: listarUsuarios,
  });
}
