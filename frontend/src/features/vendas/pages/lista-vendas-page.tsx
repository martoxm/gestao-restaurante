"use client";

import { useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { Plus, ShoppingCart } from "lucide-react";
import { useVendas } from "@/features/vendas/hooks/useVendas";
import { StatusVendaBadge } from "@/features/vendas/components/status-venda-badge";
import {
  codigoVenda,
  estaEmAndamento,
  formatarDataHora,
  formatarMoeda,
  type VendaItemLista,
} from "@/features/vendas/types/venda";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { cn } from "@/lib/utils";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";

const FILTROS = {
  todas: { rotulo: "Todas", filtro: () => true },
  andamento: { rotulo: "Em andamento", filtro: (v: VendaItemLista) => estaEmAndamento(v.status) },
  confirmadas: { rotulo: "Confirmadas", filtro: (v: VendaItemLista) => v.status === "Confirmada" },
  semEstoque: {
    rotulo: "Sem estoque",
    filtro: (v: VendaItemLista) => v.status === "Cancelada" && v.motivoCancelamento === "EstoqueInsuficiente",
  },
  pagamento: {
    rotulo: "Pagamento recusado",
    filtro: (v: VendaItemLista) =>
      v.status === "Cancelada" && v.motivoCancelamento !== "EstoqueInsuficiente",
  },
} as const;

type ChaveFiltro = keyof typeof FILTROS;

export function ListaVendasPage() {
  const router = useRouter();
  const { data, isLoading, isError } = useVendas();
  const [filtro, setFiltro] = useState<ChaveFiltro>("todas");

  const vendas = data?.itens ?? [];
  const visiveis = vendas.filter(FILTROS[filtro].filtro);

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <div>
          <h1 className="font-heading text-lg font-semibold">Vendas</h1>
          <p className="text-sm text-muted-foreground">
            As vendas em andamento se atualizam sozinhas.
          </p>
        </div>
        <Button asChild>
          <Link href="/vendas/nova">
            <Plus /> Nova venda
          </Link>
        </Button>
      </div>

      <div className="flex flex-wrap gap-2" role="tablist" aria-label="Filtrar vendas">
        {(Object.keys(FILTROS) as ChaveFiltro[]).map((chave) => {
          const quantidade = vendas.filter(FILTROS[chave].filtro).length;
          return (
            <button
              key={chave}
              type="button"
              role="tab"
              aria-selected={filtro === chave}
              onClick={() => setFiltro(chave)}
              className={cn(
                "rounded-full border px-3 py-1 text-sm transition-colors",
                filtro === chave
                  ? "border-primary bg-primary text-primary-foreground"
                  : "hover:bg-muted"
              )}
            >
              {FILTROS[chave].rotulo}
              <span className="ml-1.5 tabular-nums opacity-70">{quantidade}</span>
            </button>
          );
        })}
      </div>

      {isLoading && (
        <div className="flex flex-col gap-2">
          {Array.from({ length: 6 }).map((_, i) => (
            <Skeleton key={i} className="h-10 w-full" />
          ))}
        </div>
      )}

      {isError && (
        <Card>
          <CardContent className="py-8 text-center text-sm text-destructive">
            Não foi possível carregar as vendas. Verifique se a API de Vendas está rodando.
          </CardContent>
        </Card>
      )}

      {data && vendas.length === 0 && (
        <Card>
          <CardContent className="flex flex-col items-center gap-3 py-12 text-center">
            <ShoppingCart className="size-8 text-muted-foreground" />
            <p className="text-sm text-muted-foreground">Nenhuma venda registrada ainda.</p>
            <Button asChild>
              <Link href="/vendas/nova">Registrar a primeira venda</Link>
            </Button>
          </CardContent>
        </Card>
      )}

      {data && vendas.length > 0 && (
        <Card className="py-0">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Venda</TableHead>
                <TableHead>Data</TableHead>
                <TableHead className="text-right">Itens</TableHead>
                <TableHead className="text-right">Total</TableHead>
                <TableHead>Situação</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {visiveis.length === 0 && (
                <TableRow>
                  <TableCell colSpan={5} className="py-8 text-center text-muted-foreground">
                    Nenhuma venda neste filtro.
                  </TableCell>
                </TableRow>
              )}

              {visiveis.map((venda) => (
                <TableRow
                  key={venda.id}
                  tabIndex={0}
                  className="cursor-pointer"
                  onClick={() => router.push(`/vendas/${venda.id}`)}
                  onKeyDown={(e) => e.key === "Enter" && router.push(`/vendas/${venda.id}`)}
                >
                  <TableCell className="font-mono text-xs">{codigoVenda(venda.id)}</TableCell>
                  <TableCell>{formatarDataHora(venda.criadoEm)}</TableCell>
                  <TableCell className="text-right tabular-nums">{venda.quantidadeItens}</TableCell>
                  <TableCell className="text-right font-medium tabular-nums">
                    {formatarMoeda(venda.total)}
                  </TableCell>
                  <TableCell>
                    <StatusVendaBadge status={venda.status} motivo={venda.motivoCancelamento} />
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </Card>
      )}
    </div>
  );
}
