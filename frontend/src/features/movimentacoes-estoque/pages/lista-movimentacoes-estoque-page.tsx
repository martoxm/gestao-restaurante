"use client";

import { useState } from "react";
import Link from "next/link";
import { ArrowDownLeft, ArrowRightLeft, ArrowUpRight, Plus } from "lucide-react";
import { useMovimentacoesEstoque } from "@/features/movimentacoes-estoque/hooks/useMovimentacoesEstoque";
import { useProdutos } from "@/features/produtos/hooks/useProdutos";
import type { TipoMovimentacaoEstoque } from "@/features/movimentacoes-estoque/types/movimentacao-estoque";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { cn } from "@/lib/utils";

const TAMANHO_PAGINA = 15;

const TODOS_PRODUTOS = "todos";

const FILTROS_TIPO: { valor: TipoMovimentacaoEstoque | undefined; rotulo: string }[] = [
  { valor: undefined, rotulo: "Todas" },
  { valor: "Entrada", rotulo: "Entradas" },
  { valor: "Saida", rotulo: "Saídas" },
];

function formatarDataHora(data: string) {
  return new Date(data).toLocaleString("pt-BR", { dateStyle: "short", timeStyle: "short" });
}

export function ListaMovimentacoesEstoquePage() {
  const [pagina, setPagina] = useState(1);
  const [produtoId, setProdutoId] = useState<string | undefined>();
  const [tipo, setTipo] = useState<TipoMovimentacaoEstoque | undefined>();
  const [dataInicio, setDataInicio] = useState("");
  const [dataFim, setDataFim] = useState("");

  const { data, isLoading, isError, isFetching } = useMovimentacoesEstoque({
    pagina,
    tamanhoPagina: TAMANHO_PAGINA,
    produtoId,
    tipo,
    dataInicio: dataInicio || undefined,
    dataFim: dataFim || undefined,
  });
  const { data: produtos } = useProdutos({ tamanhoPagina: 100 });

  const temFiltro = !!produtoId || !!tipo || !!dataInicio || !!dataFim;

  function filtrar(acao: () => void) {
    acao();
    setPagina(1);
  }

  function limparFiltros() {
    filtrar(() => {
      setProdutoId(undefined);
      setTipo(undefined);
      setDataInicio("");
      setDataFim("");
    });
  }

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <div>
          <h1 className="font-heading text-lg font-semibold">Movimentações</h1>
          <p className="text-sm text-muted-foreground">
            Histórico de entradas e saídas, da mais recente para a mais antiga.
          </p>
        </div>
        <Button asChild>
          <Link href="/movimentacoes/nova">
            <Plus /> Nova movimentação
          </Link>
        </Button>
      </div>

      <Card>
        <CardContent className="flex flex-wrap items-end gap-4">
          <div className="flex min-w-48 flex-1 flex-col gap-1.5">
            <Label htmlFor="filtro-produto">Produto</Label>
            <Select
              value={produtoId ?? TODOS_PRODUTOS}
              onValueChange={(valor) =>
                filtrar(() => setProdutoId(valor === TODOS_PRODUTOS ? undefined : valor))
              }
            >
              <SelectTrigger id="filtro-produto" className="w-full">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value={TODOS_PRODUTOS}>Todos os produtos</SelectItem>
                {produtos?.itens.map((produto) => (
                  <SelectItem key={produto.id} value={produto.id}>
                    {produto.nome}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="filtro-inicio">De</Label>
            <Input
              id="filtro-inicio"
              type="date"
              value={dataInicio}
              max={dataFim || undefined}
              onChange={(e) => filtrar(() => setDataInicio(e.target.value))}
            />
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="filtro-fim">Até</Label>
            <Input
              id="filtro-fim"
              type="date"
              value={dataFim}
              min={dataInicio || undefined}
              onChange={(e) => filtrar(() => setDataFim(e.target.value))}
            />
          </div>

          <div className="flex flex-wrap gap-2" role="tablist" aria-label="Filtrar por tipo">
            {FILTROS_TIPO.map((opcao) => (
              <button
                key={opcao.rotulo}
                type="button"
                role="tab"
                aria-selected={tipo === opcao.valor}
                onClick={() => filtrar(() => setTipo(opcao.valor))}
                className={cn(
                  "h-9 rounded-full border px-3 text-sm transition-colors",
                  tipo === opcao.valor
                    ? "border-primary bg-primary text-primary-foreground"
                    : "hover:bg-muted"
                )}
              >
                {opcao.rotulo}
              </button>
            ))}
          </div>

          {temFiltro && (
            <Button variant="ghost" onClick={limparFiltros}>
              Limpar filtros
            </Button>
          )}
        </CardContent>
      </Card>

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
            Não foi possível carregar as movimentações. Verifique se a API de Estoque está rodando.
          </CardContent>
        </Card>
      )}

      {data && data.totalItens === 0 && (
        <Card>
          <CardContent className="flex flex-col items-center gap-3 py-12 text-center">
            <ArrowRightLeft className="size-8 text-muted-foreground" />
            <p className="text-sm text-muted-foreground">
              {temFiltro
                ? "Nenhuma movimentação com esses filtros."
                : "Nenhuma movimentação registrada ainda."}
            </p>
            {temFiltro ? (
              <Button variant="outline" onClick={limparFiltros}>
                Limpar filtros
              </Button>
            ) : (
              <Button asChild>
                <Link href="/movimentacoes/nova">Registrar a primeira</Link>
              </Button>
            )}
          </CardContent>
        </Card>
      )}

      {data && data.totalItens > 0 && (
        <>
          <Card className={cn("py-0 transition-opacity", isFetching && "opacity-60")}>
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Data</TableHead>
                  <TableHead>Produto</TableHead>
                  <TableHead>Tipo</TableHead>
                  <TableHead className="text-right">Quantidade</TableHead>
                  <TableHead>Observações</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {data.itens.map((movimentacao) => {
                  const entrada = movimentacao.tipo === "Entrada";
                  return (
                    <TableRow key={movimentacao.id}>
                      <TableCell className="whitespace-nowrap">
                        {formatarDataHora(movimentacao.dataMovimentacao)}
                      </TableCell>
                      <TableCell className="font-medium">{movimentacao.nomeProduto}</TableCell>
                      <TableCell>
                        <span
                          className={cn(
                            "inline-flex items-center gap-1 text-sm",
                            entrada
                              ? "text-emerald-700 dark:text-emerald-400"
                              : "text-red-700 dark:text-red-400"
                          )}
                        >
                          {entrada ? (
                            <ArrowDownLeft className="size-4" />
                          ) : (
                            <ArrowUpRight className="size-4" />
                          )}
                          {entrada ? "Entrada" : "Saída"}
                        </span>
                      </TableCell>
                      <TableCell className="text-right font-medium tabular-nums">
                        {entrada ? "+" : "−"}
                        {movimentacao.quantidade}
                      </TableCell>
                      <TableCell className="max-w-64 truncate text-muted-foreground">
                        {movimentacao.observacoes || "—"}
                      </TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          </Card>

          <div className="flex items-center justify-between gap-2 text-sm text-muted-foreground">
            <span>
              {data.totalItens} {data.totalItens === 1 ? "movimentação" : "movimentações"}
            </span>
            <div className="flex items-center gap-2">
              <Button
                variant="outline"
                size="sm"
                disabled={pagina <= 1}
                onClick={() => setPagina((p) => p - 1)}
              >
                Anterior
              </Button>
              <span className="tabular-nums">
                Página {data.pagina} de {data.totalPaginas}
              </span>
              <Button
                variant="outline"
                size="sm"
                disabled={pagina >= data.totalPaginas}
                onClick={() => setPagina((p) => p + 1)}
              >
                Próxima
              </Button>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
