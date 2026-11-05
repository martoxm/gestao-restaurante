"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { AlertTriangle, ArrowLeft, CreditCard, PackageX, Plus } from "lucide-react";
import { useVenda } from "@/features/vendas/hooks/useVenda";
import { useAvisoMudancaStatus } from "@/features/vendas/hooks/useAvisoMudancaStatus";
import { StatusVendaBadge } from "@/features/vendas/components/status-venda-badge";
import { LinhaTempoVenda } from "@/features/vendas/components/linha-tempo-venda";
import {
  codigoVenda,
  estaEmAndamento,
  explicarCancelamento,
  formatarDataHora,
  formatarMoeda,
  type VendaDetalhes,
} from "@/features/vendas/types/venda";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Table,
  TableBody,
  TableCell,
  TableFooter,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";

export function DetalhesVendaPage() {
  const { id } = useParams<{ id: string }>();
  const { data: venda, isLoading, isError } = useVenda(id);
  useAvisoMudancaStatus(venda);

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <div>
          <h1 className="font-heading text-lg font-semibold">
            Venda {venda ? codigoVenda(venda.numero) : ""}
          </h1>
          {venda && <p className="text-sm text-muted-foreground">{formatarDataHora(venda.criadoEm)}</p>}
        </div>
        <div className="flex gap-2">
          <Button variant="outline" asChild>
            <Link href="/vendas">
              <ArrowLeft /> Vendas
            </Link>
          </Button>
          <Button asChild>
            <Link href="/vendas/nova">
              <Plus /> Nova venda
            </Link>
          </Button>
        </div>
      </div>

      {isLoading && (
        <div className="flex flex-col gap-4">
          <Skeleton className="h-28 w-full" />
          <Skeleton className="h-48 w-full" />
        </div>
      )}

      {isError && (
        <Card>
          <CardContent className="py-8 text-center text-sm text-muted-foreground">
            Venda não encontrada. Ela pode ser de outro restaurante ou o endereço está errado.
          </CardContent>
        </Card>
      )}

      {venda && (
        <>
          <Card>
            <CardHeader className="flex flex-row flex-wrap items-center justify-between gap-2">
              <CardTitle className="flex items-center gap-3 text-2xl tabular-nums">
                {formatarMoeda(venda.total)}
                <StatusVendaBadge status={venda.status} motivo={venda.motivoCancelamento} />
              </CardTitle>
              {estaEmAndamento(venda.status) && (
                <p className="text-sm text-muted-foreground">
                  {venda.status === "Pendente"
                    ? "Reservando o estoque..."
                    : "Aguardando a resposta da operadora do cartão..."}
                </p>
              )}
            </CardHeader>
            <CardContent>
              <LinhaTempoVenda venda={venda} />
            </CardContent>
          </Card>

          {venda.status === "Cancelada" && <AvisoCancelamento venda={venda} />}

          <Card>
            <CardHeader>
              <CardTitle>Itens</CardTitle>
            </CardHeader>
            <CardContent>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Produto</TableHead>
                    <TableHead className="text-right">Preço unit.</TableHead>
                    <TableHead className="text-right">Qtd.</TableHead>
                    <TableHead className="text-right">Subtotal</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {venda.itens.map((item) => (
                    <TableRow key={item.produtoId}>
                      <TableCell>{item.nomeProduto}</TableCell>
                      <TableCell className="text-right tabular-nums">{formatarMoeda(item.precoUnitario)}</TableCell>
                      <TableCell className="text-right tabular-nums">{item.quantidade}</TableCell>
                      <TableCell className="text-right tabular-nums">{formatarMoeda(item.subtotal)}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
                <TableFooter>
                  <TableRow>
                    <TableCell colSpan={3}>Total</TableCell>
                    <TableCell className="text-right tabular-nums">{formatarMoeda(venda.total)}</TableCell>
                  </TableRow>
                </TableFooter>
              </Table>
            </CardContent>
          </Card>
        </>
      )}
    </div>
  );
}

function AvisoCancelamento({ venda }: { venda: VendaDetalhes }) {
  const explicacao = explicarCancelamento(venda.motivoCancelamento, venda.detalheCancelamento);
  const Icone =
    venda.motivoCancelamento === "EstoqueInsuficiente"
      ? PackageX
      : venda.motivoCancelamento
        ? CreditCard
        : AlertTriangle;

  return (
    <div role="alert" className="flex gap-3 rounded-xl border border-destructive/30 bg-destructive/5 p-4">
      <Icone className="mt-0.5 size-5 shrink-0 text-destructive" />
      <div className="flex flex-col gap-1">
        <p className="font-medium text-destructive">{explicacao.titulo}</p>
        <p className="text-sm">{explicacao.descricao}</p>
        <p className="text-sm text-muted-foreground">{explicacao.oQueFazer}</p>
        {venda.motivoCancelamento === "EstoqueInsuficiente" && (
          <Button variant="outline" size="sm" className="mt-2 self-start" asChild>
            <Link href="/produtos">Ver estoque dos produtos</Link>
          </Button>
        )}
      </div>
    </div>
  );
}
