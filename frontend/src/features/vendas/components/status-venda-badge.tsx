import { CheckCircle2, CircleDashed, CreditCard, XCircle } from "lucide-react";
import { cn } from "@/lib/utils";
import {
  explicarCancelamento,
  ROTULO_STATUS,
  type MotivoCancelamento,
  type StatusVenda,
} from "@/features/vendas/types/venda";

const ESTILO: Record<StatusVenda, { classe: string; icone: typeof CheckCircle2 }> = {
  Pendente: { classe: "bg-muted text-muted-foreground", icone: CircleDashed },
  AguardandoPagamento: {
    classe: "bg-amber-100 text-amber-900 dark:bg-amber-900/40 dark:text-amber-200",
    icone: CreditCard,
  },
  Confirmada: {
    classe: "bg-emerald-100 text-emerald-900 dark:bg-emerald-900/40 dark:text-emerald-200",
    icone: CheckCircle2,
  },
  Cancelada: {
    classe: "bg-red-100 text-red-900 dark:bg-red-900/40 dark:text-red-200",
    icone: XCircle,
  },
};

export function StatusVendaBadge({
  status,
  motivo,
}: {
  status: StatusVenda;
  motivo?: MotivoCancelamento | null;
}) {
  const { classe, icone: Icone } = ESTILO[status];
  const emAndamento = status === "Pendente" || status === "AguardandoPagamento";
  const rotulo =
    status === "Cancelada" && motivo ? explicarCancelamento(motivo).rotuloCurto : ROTULO_STATUS[status];

  return (
    <span
      className={cn(
        "inline-flex items-center gap-1 whitespace-nowrap rounded-full px-2 py-0.5 text-xs font-medium",
        classe
      )}
    >
      <Icone className={cn("size-3.5", emAndamento && "animate-pulse")} />
      {rotulo}
    </span>
  );
}
