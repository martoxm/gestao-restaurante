"use client";

import { useEffect, useState } from "react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { formatarData } from "@/lib/datas";
import { formatarDataHora } from "@/features/vendas/types/venda";
import { ResumoPlataforma } from "@/features/plataforma/components/resumo-plataforma";
import { DetalhesRestauranteDialog } from "@/features/plataforma/components/detalhes-restaurante-dialog";
import { useAuditoria, useRestaurantesPlataforma } from "@/features/plataforma/hooks/usePlataforma";
import {
  ROTULO_PLANO,
  ROTULO_SITUACAO,
  type PlanoRestaurante,
  type SituacaoRestaurante,
} from "@/features/plataforma/types/plataforma";

const TODOS = "todos";

const COR_SITUACAO: Record<SituacaoRestaurante, "default" | "secondary" | "destructive" | "outline"> = {
  Pago: "default",
  EmTeste: "secondary",
  VencendoEmBreve: "outline",
  TesteVencido: "destructive",
  PlanoVencido: "destructive",
  Inativo: "destructive",
};

export function PlataformaPage() {
  const [buscaDigitada, setBuscaDigitada] = useState("");
  const [busca, setBusca] = useState("");
  const [plano, setPlano] = useState<string>(TODOS);
  const [status, setStatus] = useState<string>(TODOS);
  const [pagina, setPagina] = useState(1);
  const [paginaAuditoria, setPaginaAuditoria] = useState(1);
  const [selecionado, setSelecionado] = useState<string | null>(null);

  useEffect(() => {
    const t = setTimeout(() => {
      setBusca(buscaDigitada.trim());
      setPagina(1);
    }, 400);
    return () => clearTimeout(t);
  }, [buscaDigitada]);

  const restaurantes = useRestaurantesPlataforma({
    busca: busca || undefined,
    plano: plano === TODOS ? undefined : (plano as PlanoRestaurante),
    status: status === TODOS ? undefined : (status as SituacaoRestaurante | "Ativo"),
    pagina,
    tamanhoPagina: 10,
  });
  const auditoria = useAuditoria(paginaAuditoria);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Plataforma</h1>
        <p className="text-sm text-muted-foreground">
          Visão de todos os clientes. Toda ação aqui fica registrada na auditoria.
        </p>
      </div>

      <ResumoPlataforma />

      <Card>
        <CardHeader>
          <CardTitle>Restaurantes</CardTitle>
        </CardHeader>
        <CardContent className="space-y-3">
          <div className="flex flex-col gap-2 sm:flex-row">
            <Input
              placeholder="Buscar por nome ou e-mail"
              value={buscaDigitada}
              onChange={(e) => setBuscaDigitada(e.target.value)}
            />
            <Select value={plano} onValueChange={(v) => { setPlano(v); setPagina(1); }}>
              <SelectTrigger className="sm:w-40">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value={TODOS}>Todos os planos</SelectItem>
                {(Object.keys(ROTULO_PLANO) as PlanoRestaurante[]).map((p) => (
                  <SelectItem key={p} value={p}>
                    {ROTULO_PLANO[p]}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            <Select value={status} onValueChange={(v) => { setStatus(v); setPagina(1); }}>
              <SelectTrigger className="sm:w-48">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value={TODOS}>Todas as situações</SelectItem>
                <SelectItem value="Ativo">Ativo</SelectItem>
                <SelectItem value="Assinante">Assinantes</SelectItem>
                {(Object.keys(ROTULO_SITUACAO) as SituacaoRestaurante[]).map((s) => (
                  <SelectItem key={s} value={s}>
                    {ROTULO_SITUACAO[s]}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          {restaurantes.isLoading ? (
            <Skeleton className="h-48 w-full" />
          ) : restaurantes.isError || !restaurantes.data ? (
            <p className="text-sm text-destructive">Não foi possível carregar os restaurantes.</p>
          ) : restaurantes.data.itens.length === 0 ? (
            <p className="py-6 text-center text-sm text-muted-foreground">Nenhum restaurante encontrado.</p>
          ) : (
            <div className="overflow-x-auto">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Nome</TableHead>
                    <TableHead>Dono</TableHead>
                    <TableHead>Plano</TableHead>
                    <TableHead>Situação</TableHead>
                    <TableHead>Fim do teste</TableHead>
                    <TableHead>Assinou em</TableHead>
                    <TableHead>Válido até</TableHead>
                    <TableHead className="text-right">Usuários</TableHead>
                    <TableHead>Criado em</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {restaurantes.data.itens.map((r) => (
                    <TableRow key={r.id} className="cursor-pointer" onClick={() => setSelecionado(r.id)}>
                      <TableCell className="font-medium">{r.nome}</TableCell>
                      <TableCell className="text-muted-foreground">{r.emailDono ?? "—"}</TableCell>
                      <TableCell>{ROTULO_PLANO[r.plano]}</TableCell>
                      <TableCell>
                        <Badge variant={COR_SITUACAO[r.situacao]}>{ROTULO_SITUACAO[r.situacao]}</Badge>
                      </TableCell>
                      <TableCell>{r.testeTerminaEm ? formatarData(r.testeTerminaEm) : "—"}</TableCell>
                      <TableCell>{r.assinadoEm ? formatarData(r.assinadoEm) : "—"}</TableCell>
                      <TableCell>{r.planoValidoAte ? formatarData(r.planoValidoAte) : r.plano !== "Teste" ? "Sem vencimento" : "—"}</TableCell>
                      <TableCell className="text-right tabular-nums">{r.quantidadeUsuarios}</TableCell>
                      <TableCell>{formatarData(r.criadoEm)}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          )}

          {restaurantes.data && restaurantes.data.totalPaginas > 1 && (
            <Paginacao pagina={pagina} total={restaurantes.data.totalPaginas} aoMudar={setPagina} />
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Auditoria</CardTitle>
        </CardHeader>
        <CardContent className="space-y-3">
          {auditoria.isLoading ? (
            <Skeleton className="h-32 w-full" />
          ) : !auditoria.data || auditoria.data.itens.length === 0 ? (
            <p className="text-sm text-muted-foreground">Nenhuma ação registrada ainda.</p>
          ) : (
            <div className="overflow-x-auto">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Quando</TableHead>
                    <TableHead>Quem</TableHead>
                    <TableHead>Ação</TableHead>
                    <TableHead>Restaurante</TableHead>
                    <TableHead>Antes → depois</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {auditoria.data.itens.map((a) => (
                    <TableRow key={a.id}>
                      <TableCell className="whitespace-nowrap">{formatarDataHora(a.ocorridoEm)}</TableCell>
                      <TableCell>{a.emailSuperAdmin}</TableCell>
                      <TableCell>{a.acao}</TableCell>
                      <TableCell>{a.nomeRestaurante ?? "—"}</TableCell>
                      <TableCell className="text-muted-foreground">
                        {a.valorAntes ?? "—"} → {a.valorDepois ?? "—"}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          )}
          {auditoria.data && auditoria.data.totalPaginas > 1 && (
            <Paginacao pagina={paginaAuditoria} total={auditoria.data.totalPaginas} aoMudar={setPaginaAuditoria} />
          )}
        </CardContent>
      </Card>

      <DetalhesRestauranteDialog id={selecionado} aoFechar={() => setSelecionado(null)} />
    </div>
  );
}

function Paginacao({ pagina, total, aoMudar }: { pagina: number; total: number; aoMudar: (p: number) => void }) {
  return (
    <div className="flex items-center justify-end gap-2 text-sm">
      <Button size="sm" variant="outline" disabled={pagina <= 1} onClick={() => aoMudar(pagina - 1)}>
        Anterior
      </Button>
      <span className="tabular-nums">
        {pagina} de {total}
      </span>
      <Button size="sm" variant="outline" disabled={pagina >= total} onClick={() => aoMudar(pagina + 1)}>
        Próxima
      </Button>
    </div>
  );
}
