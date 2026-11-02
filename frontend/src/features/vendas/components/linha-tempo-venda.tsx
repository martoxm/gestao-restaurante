import { Check, Loader2, X } from "lucide-react";
import { cn } from "@/lib/utils";
import type { VendaDetalhes } from "@/features/vendas/types/venda";

type EstadoPasso = "feito" | "atual" | "falhou" | "esperando";

const TITULOS = ["Venda registrada", "Estoque reservado", "Pagamento aprovado", "Venda confirmada"];

function montarPassos(venda: VendaDetalhes): EstadoPasso[] {
  let feitos = 1;
  let falhou: number | null = null;

  if (venda.status === "AguardandoPagamento") feitos = 2;
  if (venda.status === "Confirmada") feitos = 4;
  if (venda.status === "Cancelada") {
    if (venda.motivoCancelamento === "EstoqueInsuficiente") falhou = 1;
    else if (venda.motivoCancelamento) falhou = 2;
    else falhou = 3;
    feitos = falhou;
  }

  return TITULOS.map((_, indice) => {
    if (indice < feitos) return "feito";
    if (falhou === indice) return "falhou";
    if (falhou === null && indice === feitos) return "atual";
    return "esperando";
  });
}

const ESTILO_ICONE: Record<EstadoPasso, string> = {
  feito: "bg-emerald-600 text-white",
  atual: "bg-primary/10 text-primary ring-2 ring-primary/40",
  falhou: "bg-destructive text-white",
  esperando: "bg-muted text-muted-foreground",
};

export function LinhaTempoVenda({ venda }: { venda: VendaDetalhes }) {
  const estados = montarPassos(venda);

  return (
    <ol className="grid grid-cols-2 gap-4 sm:grid-cols-4">
      {TITULOS.map((titulo, indice) => {
        const estado = estados[indice];
        return (
          <li key={titulo} className="flex items-center gap-2">
            <span
              className={cn(
                "flex size-7 shrink-0 items-center justify-center rounded-full text-xs",
                ESTILO_ICONE[estado]
              )}
            >
              {estado === "feito" && <Check className="size-4" />}
              {estado === "atual" && <Loader2 className="size-4 animate-spin" />}
              {estado === "falhou" && <X className="size-4" />}
              {estado === "esperando" && indice + 1}
            </span>
            <span
              className={cn(
                "text-sm",
                estado === "esperando" && "text-muted-foreground",
                estado === "falhou" && "font-medium text-destructive"
              )}
            >
              {titulo}
            </span>
          </li>
        );
      })}
    </ol>
  );
}
