"use client";

import { Bar, BarChart, CartesianGrid, XAxis, YAxis } from "recharts";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { ChartContainer, ChartTooltip, ChartTooltipContent, type ChartConfig } from "@/components/ui/chart";
import { Skeleton } from "@/components/ui/skeleton";
import { formatarMoeda } from "@/features/vendas/types/venda";
import { useResumoPlataforma } from "@/features/plataforma/hooks/usePlataforma";

const chartConfig = { quantidade: { label: "Cadastros", color: "var(--chart-1)" } } satisfies ChartConfig;

function Numero({ titulo, valor, detalhe }: { titulo: string; valor: string | number; detalhe?: string }) {
  return (
    <Card>
      <CardHeader className="pb-2">
        <CardDescription>{titulo}</CardDescription>
        <CardTitle className="text-3xl tabular-nums">{valor}</CardTitle>
      </CardHeader>
      {detalhe && <CardContent className="text-xs text-muted-foreground">{detalhe}</CardContent>}
    </Card>
  );
}

export function ResumoPlataforma() {
  const { data, isLoading, isError } = useResumoPlataforma();

  if (isLoading) return <Skeleton className="h-64 w-full" />;
  if (isError || !data) return <p className="text-sm text-destructive">Não foi possível carregar o resumo.</p>;

  const { restaurantes: r, usuarios: u, convites: c, funil: f } = data;
  const etapasFunil = [
    { rotulo: "Cadastrados", valor: f.cadastrados },
    { rotulo: "Em teste", valor: f.emTeste },
    { rotulo: "Pagantes", valor: f.pagantes },
    { rotulo: "Vencidos sem assinar", valor: f.vencidosSemConverter },
  ];
  const cadastros = data.cadastrosPorDia.map((d) => ({
    dia: new Date(d.dia).toLocaleDateString("pt-BR", { day: "2-digit", month: "2-digit", timeZone: "UTC" }),
    quantidade: d.quantidade,
  }));

  return (
    <div className="space-y-4">
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <Numero titulo="Restaurantes" valor={r.total} detalhe={`${r.ativos} ativos · ${r.inativos} inativos`} />
        <Numero
          titulo="MRR estimado"
          valor={formatarMoeda(data.mrrEstimado)}
          detalhe={`Básico ${r.planoBasico} · Pro ${r.planoPro} · Teste ${r.planoTeste}`}
        />
        <Numero
          titulo="Usuários"
          valor={u.total}
          detalhe={`${u.admins} Admins · ${u.funcionarios} Funcionários · ${u.desativados} desativados`}
        />
        <Numero titulo="Convites pendentes" valor={c.pendentes} detalhe={`${c.aceitos} aceitos`} />
      </div>

      <div className="grid gap-4 sm:grid-cols-3">
        <Numero titulo="Em teste" valor={r.emTeste} />
        <Numero titulo="Vencendo em até 3 dias" valor={r.vencendoEmBreve} />
        <Numero titulo="Teste vencido" valor={r.testeVencido} />
      </div>

      <div className="grid gap-4 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>Funil de conversão</CardTitle>
            <CardDescription>Quanto de quem se cadastrou chegou em cada etapa</CardDescription>
          </CardHeader>
          <CardContent className="space-y-3">
            {etapasFunil.map((etapa) => {
              const pct = f.cadastrados ? Math.round((etapa.valor / f.cadastrados) * 100) : 0;
              return (
                <div key={etapa.rotulo} className="space-y-1">
                  <div className="flex justify-between text-sm">
                    <span>{etapa.rotulo}</span>
                    <span className="tabular-nums text-muted-foreground">
                      {etapa.valor} ({pct}%)
                    </span>
                  </div>
                  <div className="h-2 rounded bg-muted">
                    <div className="h-2 rounded bg-primary" style={{ width: `${pct}%` }} />
                  </div>
                </div>
              );
            })}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Cadastros por dia</CardTitle>
            <CardDescription>Últimos 30 dias</CardDescription>
          </CardHeader>
          <CardContent>
            <ChartContainer config={chartConfig} className="h-56 w-full">
              <BarChart data={cadastros}>
                <CartesianGrid vertical={false} />
                <XAxis dataKey="dia" tickLine={false} axisLine={false} minTickGap={16} />
                <YAxis allowDecimals={false} width={28} tickLine={false} axisLine={false} />
                <ChartTooltip content={<ChartTooltipContent />} />
                <Bar dataKey="quantidade" fill="var(--color-quantidade)" radius={4} />
              </BarChart>
            </ChartContainer>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
