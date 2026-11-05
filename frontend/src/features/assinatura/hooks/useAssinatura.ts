import { useMutation, useQuery } from "@tanstack/react-query";
import { toast } from "sonner";
import { obterMensagemErro } from "@/lib/api-errors";
import { iniciarAssinatura, listarPlanos } from "@/features/assinatura/services/assinatura";

export function usePlanos() {
  return useQuery({ queryKey: ["assinatura", "planos"], queryFn: listarPlanos });
}

export function useIniciarAssinatura() {
  return useMutation({
    mutationFn: iniciarAssinatura,
    onSuccess: (resposta) => {
      window.location.href = resposta.urlPagamento;
    },
    onError: (error) => {
      toast.error(obterMensagemErro(error, "Não foi possível abrir o pagamento. Tente de novo."));
    },
  });
}
