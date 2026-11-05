import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { obterMensagemErro } from "@/lib/api-errors";
import {
  listarAuditoria,
  listarRestaurantesPlataforma,
  obterRestaurantePlataforma,
  obterResumoPlataforma,
} from "@/features/plataforma/services/plataforma";
import type { FiltroRestaurantesPlataforma } from "@/features/plataforma/types/plataforma";

export function useResumoPlataforma() {
  return useQuery({ queryKey: ["plataforma", "resumo"], queryFn: obterResumoPlataforma });
}

export function useRestaurantesPlataforma(filtro: FiltroRestaurantesPlataforma) {
  return useQuery({
    queryKey: ["plataforma", "restaurantes", filtro],
    queryFn: () => listarRestaurantesPlataforma(filtro),
    placeholderData: keepPreviousData,
  });
}

export function useRestaurantePlataforma(id: string | null) {
  return useQuery({
    queryKey: ["plataforma", "restaurante", id],
    queryFn: () => obterRestaurantePlataforma(id!),
    enabled: !!id,
  });
}

export function useAuditoria(pagina: number) {
  return useQuery({
    queryKey: ["plataforma", "auditoria", pagina],
    queryFn: () => listarAuditoria(pagina),
    placeholderData: keepPreviousData,
  });
}

export function useAcaoPlataforma() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ executar }: { executar: () => Promise<void>; sucesso: string }) => executar(),
    onSuccess: (_, { sucesso }) => {
      queryClient.invalidateQueries({ queryKey: ["plataforma"] });
      toast.success(sucesso);
    },
    onError: (error) => toast.error(obterMensagemErro(error, "Não foi possível concluir a ação.")),
  });
}
