import { useVendas } from "@/features/vendas/hooks/useVendas";
import type { VendaItemLista } from "@/features/vendas/types/venda";

export interface FaturamentoDia {
  dia: string;
  faturamento: number;
  vendas: number;
}

export interface ResumoVendas {
  faturamentoHoje: number;
  vendasHoje: number;
  faturamento7Dias: number;
  ticketMedio: number;
  confirmadas: number;
  emAndamento: number;
  canceladasSemEstoque: number;
  canceladasPagamento: number;
  taxaCancelamento: number;
  porDia: FaturamentoDia[];
  ultimas: VendaItemLista[];
}

function inicioDoDia(data: Date) {
  return new Date(data.getFullYear(), data.getMonth(), data.getDate());
}

export function useResumoVendas() {
  const consulta = useVendas();
  const vendas = consulta.data?.itens ?? [];

  const hoje = inicioDoDia(new Date());
  const seteDiasAtras = new Date(hoje);
  seteDiasAtras.setDate(hoje.getDate() - 6);

  const confirmadas = vendas.filter((v) => v.status === "Confirmada");
  const confirmadasHoje = confirmadas.filter((v) => new Date(v.criadoEm) >= hoje);
  const confirmadas7Dias = confirmadas.filter((v) => new Date(v.criadoEm) >= seteDiasAtras);
  const canceladas = vendas.filter((v) => v.status === "Cancelada");
  const finalizadas = confirmadas.length + canceladas.length;

  const porDia: FaturamentoDia[] = Array.from({ length: 7 }, (_, i) => {
    const dia = new Date(seteDiasAtras);
    dia.setDate(seteDiasAtras.getDate() + i);
    const doDia = confirmadas7Dias.filter(
      (v) => inicioDoDia(new Date(v.criadoEm)).getTime() === dia.getTime()
    );
    return {
      dia: dia.toLocaleDateString("pt-BR", { weekday: "short", day: "2-digit" }),
      faturamento: doDia.reduce((soma, v) => soma + v.total, 0),
      vendas: doDia.length,
    };
  });

  const resumo: ResumoVendas = {
    faturamentoHoje: confirmadasHoje.reduce((soma, v) => soma + v.total, 0),
    vendasHoje: confirmadasHoje.length,
    faturamento7Dias: confirmadas7Dias.reduce((soma, v) => soma + v.total, 0),
    ticketMedio: confirmadas.length
      ? confirmadas.reduce((soma, v) => soma + v.total, 0) / confirmadas.length
      : 0,
    confirmadas: confirmadas.length,
    emAndamento: vendas.length - finalizadas,
    canceladasSemEstoque: canceladas.filter((v) => v.motivoCancelamento === "EstoqueInsuficiente").length,
    canceladasPagamento: canceladas.filter((v) => v.motivoCancelamento && v.motivoCancelamento !== "EstoqueInsuficiente").length,
    taxaCancelamento: finalizadas ? canceladas.length / finalizadas : 0,
    porDia,
    ultimas: vendas.slice(0, 5),
  };

  return { ...consulta, resumo };
}
