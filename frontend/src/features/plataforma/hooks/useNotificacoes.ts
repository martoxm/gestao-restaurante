import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  contarNotificacoesNaoLidas,
  listarNotificacoes,
  marcarNotificacaoLida,
  marcarTodasNotificacoesLidas,
} from "@/features/plataforma/services/plataforma";

const INTERVALO_MS = 60_000;

export function useNotificacoesNaoLidas(ativo: boolean) {
  return useQuery({
    queryKey: ["plataforma", "notificacoes", "nao-lidas"],
    queryFn: contarNotificacoesNaoLidas,
    enabled: ativo,
    refetchInterval: INTERVALO_MS,
  });
}

export function useNotificacoes(aberto: boolean, soNaoLidas: boolean) {
  return useQuery({
    queryKey: ["plataforma", "notificacoes", "lista", soNaoLidas],
    queryFn: () => listarNotificacoes(1, soNaoLidas),
    enabled: aberto,
  });
}

export function useMarcarNotificacoes() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id?: string) => (id ? marcarNotificacaoLida(id) : marcarTodasNotificacoesLidas()),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["plataforma", "notificacoes"] }),
  });
}
