"use client";

import { useAvisoSemPermissao } from "@/lib/use-aviso-sem-permissao";
import Link from "next/link";
import {
  AlertTriangle,
  CreditCard,
  DollarSign,
  Package,
  PackageX,
  Plus,
  Receipt,
  TrendingUp,
} from "lucide-react";
import { useProdutos } from "@/features/produtos/hooks/useProdutos";
import { useUsuarioAtual } from "@/features/auth/hooks/useUsuarioAtual";
import { useRestaurante } from "@/features/restaurantes/hooks/useRestaurante";
import { useDashboardCharts } from "@/features/dashboard/hooks/useDashboardCharts";
import { useResumoVendas } from "@/features/dashboard/hooks/useResumoVendas";
import { ProdutosPorCategoriaChart } from "@/features/dashboard/components/produtos-por-categoria-chart";
import { EstoquePorCategoriaChart } from "@/features/dashboard/components/estoque-por-categoria-chart";
import { FaturamentoPorDiaChart } from "@/features/dashboard/components/faturamento-por-dia-chart";
import { StatusVendaBadge } from "@/features/vendas/components/status-venda-badge";
import { codigoVenda, formatarDataHora, formatarMoeda } from "@/features/vendas/types/venda";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";

const LIMITE_ESTOQUE_BAIXO = 10;

interface Indicador {
  rotulo: string;
  valor: string;
  detalhe?: string;
  icone: typeof Package;
  href: string;
  alerta?: boolean;
}

export function DashboardPage() {
  useAvisoSemPermissao("/", "Só administradores podem gerenciar usuários.");
  const { data: produtos, isLoading: isLoadingProdutos } = useProdutos({ tamanhoPagina: 100 });
  const { restauranteId } = useUsuarioAtual();
  const { data: restaurante } = useRestaurante(restauranteId);
  const { distribuicaoCategorias, estoquePorCategoria } = useDashboardCharts();
  const { resumo, isLoading: isLoadingVendas, isError: isErroVendas } = useResumoVendas();

  const estoqueBaixo = (produtos?.itens ?? [])
    .filter((p) => p.quantidadeEmEstoque < LIMITE_ESTOQUE_BAIXO)
    .sort((a, b) => a.quantidadeEmEstoque - b.quantidadeEmEstoque);

  const indicadores: Indicador[] = [
    {
      rotulo: "Faturamento hoje",
      valor: formatarMoeda(resumo.faturamentoHoje),
      detalhe: `${resumo.vendasHoje} ${resumo.vendasHoje === 1 ? "venda confirmada" : "vendas confirmadas"}`,
      icone: DollarSign,
      href: "/vendas",
    },
    {
      rotulo: "Últimos 7 dias",
      valor: formatarMoeda(resumo.faturamento7Dias),
      detalhe: `Ticket médio ${formatarMoeda(resumo.ticketMedio)}`,
      icone: TrendingUp,
      href: "/vendas",
    },
    {
      rotulo: "Vendas canceladas",
      valor: `${Math.round(resumo.taxaCancelamento * 100)}%`,
      detalhe: `${resumo.canceladasSemEstoque} sem estoque · ${resumo.canceladasPagamento} pagamento recusado`,
      icone: Receipt,
      href: "/vendas",
      alerta: resumo.taxaCancelamento >= 0.2,
    },
    {
      rotulo: "Produtos com estoque baixo",
      valor: String(estoqueBaixo.length),
      detalhe: `Menos de ${LIMITE_ESTOQUE_BAIXO} unidades`,
      icone: AlertTriangle,
      href: "/produtos",
      alerta: estoqueBaixo.length > 0,
    },
  ];

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <div>
          <h1 className="font-heading text-lg font-semibold">Dashboard</h1>
          <p className="text-sm text-muted-foreground">
            {restaurante ? restaurante.nome : "Visão geral do restaurante"}
            {resumo.emAndamento > 0 && ` · ${resumo.emAndamento} venda(s) em andamento`}
          </p>
        </div>
        <Button asChild>
          <Link href="/vendas/nova">
            <Plus /> Nova venda
          </Link>
        </Button>
      </div>

      {isErroVendas && (
        <p className="rounded-lg border border-amber-300 bg-amber-50 p-3 text-sm text-amber-900 dark:border-amber-800 dark:bg-amber-950/40 dark:text-amber-200">
          Os números de vendas estão indisponíveis — a API de Vendas não respondeu.
        </p>
      )}

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
        {indicadores.map(({ rotulo, valor, detalhe, icone: Icone, href, alerta }, i) => {
          const carregando = i < 3 ? isLoadingVendas : isLoadingProdutos;
          return (
            <Link key={rotulo} href={href} className="group block">
              <Card className="h-full transition-shadow group-hover:shadow-md">
                <CardHeader className="flex flex-row items-center justify-between pb-2">
                  <CardTitle className="text-sm font-medium text-muted-foreground">{rotulo}</CardTitle>
                  <Icone className={alerta ? "size-4 text-destructive" : "size-4 text-muted-foreground"} />
                </CardHeader>
                <CardContent>
                  {carregando ? (
                    <Skeleton className="h-8 w-28" />
                  ) : (
                    <p className="text-2xl font-semibold tabular-nums">{valor}</p>
                  )}
                  {detalhe && !carregando && (
                    <p className="mt-1 text-xs text-muted-foreground">{detalhe}</p>
                  )}
                </CardContent>
              </Card>
            </Link>
          );
        })}
      </div>

      <div className="grid grid-cols-1 gap-4 lg:grid-cols-3">
        <div className="lg:col-span-2">
          <FaturamentoPorDiaChart data={resumo.porDia} />
        </div>

        <Card>
          <CardHeader>
            <CardTitle>Repor estoque</CardTitle>
            <CardDescription>Produtos com menos de {LIMITE_ESTOQUE_BAIXO} unidades</CardDescription>
          </CardHeader>
          <CardContent className="flex flex-col gap-2">
            {isLoadingProdutos && <Skeleton className="h-24 w-full" />}
            {!isLoadingProdutos && estoqueBaixo.length === 0 && (
              <p className="py-6 text-center text-sm text-muted-foreground">Tudo abastecido. 👍</p>
            )}
            {estoqueBaixo.slice(0, 6).map((produto) => (
              <div key={produto.id} className="flex items-center justify-between gap-2 text-sm">
                <span className="flex min-w-0 items-center gap-2">
                  <PackageX className="size-4 shrink-0 text-destructive" />
                  <span className="truncate">{produto.nome}</span>
                </span>
                <span
                  className={
                    produto.quantidadeEmEstoque === 0
                      ? "font-semibold text-destructive tabular-nums"
                      : "tabular-nums text-muted-foreground"
                  }
                >
                  {produto.quantidadeEmEstoque === 0 ? "Esgotado" : `${produto.quantidadeEmEstoque} un.`}
                </span>
              </div>
            ))}
            {estoqueBaixo.length > 0 && (
              <Button variant="outline" size="sm" className="mt-2" asChild>
                <Link href="/movimentacoes/nova">Registrar entrada</Link>
              </Button>
            )}
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader className="flex flex-row items-center justify-between">
          <div>
            <CardTitle>Últimas vendas</CardTitle>
            <CardDescription>
              {resumo.confirmadas} confirmadas no período carregado
            </CardDescription>
          </div>
          <Button variant="ghost" size="sm" asChild>
            <Link href="/vendas">Ver todas</Link>
          </Button>
        </CardHeader>
        <CardContent className="flex flex-col divide-y">
          {isLoadingVendas && <Skeleton className="h-24 w-full" />}
          {!isLoadingVendas && resumo.ultimas.length === 0 && (
            <p className="py-6 text-center text-sm text-muted-foreground">Nenhuma venda ainda.</p>
          )}
          {resumo.ultimas.map((venda) => (
            <Link
              key={venda.id}
              href={`/vendas/${venda.id}`}
              className="flex flex-wrap items-center justify-between gap-2 py-2 text-sm hover:bg-muted/50"
            >
              <span className="flex items-center gap-3">
                <CreditCard className="size-4 text-muted-foreground" />
                <span className="font-mono text-xs">{codigoVenda(venda.id)}</span>
                <span className="text-muted-foreground">{formatarDataHora(venda.criadoEm)}</span>
              </span>
              <span className="flex items-center gap-3">
                <span className="font-medium tabular-nums">{formatarMoeda(venda.total)}</span>
                <StatusVendaBadge status={venda.status} motivo={venda.motivoCancelamento} />
              </span>
            </Link>
          ))}
        </CardContent>
      </Card>

      <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
        <ProdutosPorCategoriaChart data={distribuicaoCategorias} />
        <EstoquePorCategoriaChart data={estoquePorCategoria} />
      </div>
    </div>
  );
}
