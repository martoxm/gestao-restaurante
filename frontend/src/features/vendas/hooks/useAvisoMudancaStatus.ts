import { useEffect, useRef } from "react";
import { toast } from "sonner";
import {
  codigoVenda,
  estaEmAndamento,
  explicarCancelamento,
  formatarMoeda,
  type StatusVenda,
  type VendaDetalhes,
} from "@/features/vendas/types/venda";

export function useAvisoMudancaStatus(venda: VendaDetalhes | undefined) {
  const statusAnterior = useRef<StatusVenda | undefined>(undefined);

  useEffect(() => {
    if (!venda) return;
    const anterior = statusAnterior.current;
    statusAnterior.current = venda.status;

    if (!anterior || !estaEmAndamento(anterior) || estaEmAndamento(venda.status)) return;

    if (venda.status === "Confirmada") {
      toast.success(`Venda ${codigoVenda(venda.id)} confirmada`, {
        description: `Pagamento aprovado — ${formatarMoeda(venda.total)}.`,
      });
    } else {
      const explicacao = explicarCancelamento(venda.motivoCancelamento, venda.detalheCancelamento);
      toast.error(`${explicacao.titulo} — venda ${codigoVenda(venda.id)}`, {
        description: explicacao.descricao,
      });
    }
  }, [venda]);
}
