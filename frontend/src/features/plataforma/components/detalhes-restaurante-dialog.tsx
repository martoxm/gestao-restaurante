"use client";

import { useState } from "react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { formatarData } from "@/lib/datas";
import { useAcaoPlataforma, useRestaurantePlataforma } from "@/features/plataforma/hooks/usePlataforma";
import {
  alterarAtivoRestaurante,
  alterarAtivoUsuarioPlataforma,
  estenderTeste,
  forcarLogout,
  mudarPlano,
  reenviarConvite,
} from "@/features/plataforma/services/plataforma";
import { ROTULO_PLANO, ROTULO_SITUACAO, type PlanoRestaurante } from "@/features/plataforma/types/plataforma";

export function DetalhesRestauranteDialog({ id, aoFechar }: { id: string | null; aoFechar: () => void }) {
  const { data, isLoading } = useRestaurantePlataforma(id);
  const acao = useAcaoPlataforma();
  const [dias, setDias] = useState(7);

  const executar = (executarAcao: () => Promise<void>, sucesso: string) =>
    acao.mutate({ executar: executarAcao, sucesso });

  return (
    <Dialog open={!!id} onOpenChange={(aberto) => !aberto && aoFechar()}>
      <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-3xl">
        {isLoading || !data ? (
          <>
            <DialogTitle className="sr-only">Carregando restaurante</DialogTitle>
            <Skeleton className="h-64 w-full" />
          </>
        ) : (
          <>
            <DialogHeader>
              <DialogTitle>{data.nome}</DialogTitle>
              <DialogDescription>
                {ROTULO_SITUACAO[data.situacao]} · criado em {formatarData(data.criadoEm)}
                {data.testeTerminaEm && ` · teste até ${formatarData(data.testeTerminaEm)}`}
                {data.assinadoEm && ` · assinou em ${formatarData(data.assinadoEm)}`}
                {data.ultimoPagamentoEm && ` · último pagamento ${formatarData(data.ultimoPagamentoEm)}`}
                {data.planoValidoAte && ` · válido até ${formatarData(data.planoValidoAte)}`}
                {` · limite de usuários: ${data.limiteUsuarios ?? "ilimitado"}`}
              </DialogDescription>
            </DialogHeader>

            <section className="grid gap-3 rounded-lg border p-3 sm:grid-cols-3">
              <div className="space-y-1">
                <p className="text-xs text-muted-foreground">Plano</p>
                <Select
                  value={data.plano}
                  disabled={acao.isPending}
                  onValueChange={(plano) =>
                    executar(
                      () => mudarPlano(data.id, plano as PlanoRestaurante),
                      `Plano trocado para ${ROTULO_PLANO[plano as PlanoRestaurante]}.`
                    )
                  }
                >
                  <SelectTrigger className="w-full">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {(Object.keys(ROTULO_PLANO) as PlanoRestaurante[]).map((p) => (
                      <SelectItem key={p} value={p}>
                        {ROTULO_PLANO[p]}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              <div className="space-y-1">
                <p className="text-xs text-muted-foreground">Estender teste (1 a 90 dias)</p>
                <div className="flex gap-2">
                  <Input type="number" min={1} max={90} value={dias} onChange={(e) => setDias(Number(e.target.value))} />
                  <Button
                    variant="outline"
                    disabled={acao.isPending || data.plano !== "Teste"}
                    onClick={() => executar(() => estenderTeste(data.id, dias), `Teste estendido em ${dias} dias.`)}
                  >
                    Estender
                  </Button>
                </div>
              </div>

              <div className="space-y-1">
                <p className="text-xs text-muted-foreground">Acesso do restaurante</p>
                <Button
                  className="w-full"
                  variant={data.ativo ? "destructive" : "outline"}
                  disabled={acao.isPending}
                  onClick={() =>
                    executar(
                      () => alterarAtivoRestaurante(data.id, !data.ativo),
                      data.ativo ? "Restaurante desativado." : "Restaurante reativado."
                    )
                  }
                >
                  {data.ativo ? "Desativar restaurante" : "Reativar restaurante"}
                </Button>
              </div>
            </section>

            <section className="space-y-2">
              <h3 className="font-medium">Usuários ({data.usuarios.length})</h3>
              {data.usuarios.map((u) => (
                <div key={u.id} className="flex flex-wrap items-center gap-2 rounded border p-2 text-sm">
                  <div className="min-w-0 flex-1">
                    <p className="truncate font-medium">{u.nome}</p>
                    <p className="truncate text-xs text-muted-foreground">{u.email}</p>
                  </div>
                  <Badge variant="secondary">{u.role}</Badge>
                  {!u.ativo && <Badge variant="destructive">Desativado</Badge>}
                  {!u.emailConfirmado && <Badge variant="outline">E-mail não confirmado</Badge>}
                  <Button
                    size="sm"
                    variant="outline"
                    disabled={acao.isPending}
                    onClick={() => executar(() => forcarLogout(u.id), "Sessões encerradas. A pessoa sai em até 15 minutos.")}
                  >
                    Forçar logout
                  </Button>
                  <Button
                    size="sm"
                    variant="outline"
                    disabled={acao.isPending}
                    onClick={() =>
                      executar(
                        () => alterarAtivoUsuarioPlataforma(u.id, !u.ativo),
                        u.ativo ? "Usuário desativado." : "Usuário reativado."
                      )
                    }
                  >
                    {u.ativo ? "Desativar" : "Reativar"}
                  </Button>
                </div>
              ))}
            </section>

            <section className="space-y-2">
              <h3 className="font-medium">Convites ({data.convites.length})</h3>
              {data.convites.length === 0 && <p className="text-sm text-muted-foreground">Nenhum convite.</p>}
              {data.convites.map((c) => (
                <div key={c.id} className="flex flex-wrap items-center gap-2 rounded border p-2 text-sm">
                  <span className="min-w-0 flex-1 truncate">{c.email}</span>
                  <Badge variant="secondary">{c.role}</Badge>
                  <Badge variant="outline">{c.status}</Badge>
                  {(c.status === "Pendente" || c.status === "Expirado") && (
                    <Button
                      size="sm"
                      variant="outline"
                      disabled={acao.isPending}
                      onClick={() => executar(() => reenviarConvite(c.id), "Convite reenviado com um link novo.")}
                    >
                      Reenviar
                    </Button>
                  )}
                </div>
              ))}
            </section>
          </>
        )}
      </DialogContent>
    </Dialog>
  );
}
