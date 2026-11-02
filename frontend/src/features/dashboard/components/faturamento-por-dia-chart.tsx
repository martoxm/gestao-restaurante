"use client";

import { Bar, BarChart, CartesianGrid, XAxis, YAxis } from "recharts";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import {
  ChartContainer,
  ChartTooltip,
  ChartTooltipContent,
  type ChartConfig,
} from "@/components/ui/chart";
import { formatarMoeda } from "@/features/vendas/types/venda";
import type { FaturamentoDia } from "@/features/dashboard/hooks/useResumoVendas";

const chartConfig = {
  faturamento: { label: "Faturamento", color: "var(--chart-1)" },
} satisfies ChartConfig;

export function FaturamentoPorDiaChart({ data }: { data: FaturamentoDia[] }) {
  const semVendas = data.every((d) => d.faturamento === 0);

  return (
    <Card>
      <CardHeader>
        <CardTitle>Faturamento dos últimos 7 dias</CardTitle>
        <CardDescription>Só vendas confirmadas (pagamento aprovado)</CardDescription>
      </CardHeader>
      <CardContent>
        {semVendas ? (
          <p className="py-10 text-center text-sm text-muted-foreground">
            Nenhuma venda confirmada nos últimos 7 dias.
          </p>
        ) : (
          <ChartContainer config={chartConfig} className="max-h-64 w-full">
            <BarChart data={data}>
              <CartesianGrid vertical={false} />
              <XAxis dataKey="dia" tickLine={false} axisLine={false} tickMargin={8} />
              <YAxis
                tickLine={false}
                axisLine={false}
                width={56}
                tickFormatter={(valor: number) =>
                  valor.toLocaleString("pt-BR", { notation: "compact", style: "currency", currency: "BRL" })
                }
              />
              <ChartTooltip
                content={
                  <ChartTooltipContent formatter={(valor) => formatarMoeda(Number(valor))} />
                }
              />
              <Bar dataKey="faturamento" fill="var(--color-faturamento)" radius={4} />
            </BarChart>
          </ChartContainer>
        )}
      </CardContent>
    </Card>
  );
}
