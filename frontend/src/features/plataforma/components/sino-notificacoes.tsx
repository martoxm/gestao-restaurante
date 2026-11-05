"use client";

import { useState } from "react";
import Link from "next/link";
import { Bell, CheckCheck } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { Sheet, SheetContent, SheetDescription, SheetHeader, SheetTitle, SheetTrigger } from "@/components/ui/sheet";
import { cn } from "@/lib/utils";
import { formatarDataHora } from "@/lib/datas";
import { useMarcarNotificacoes, useNotificacoes, useNotificacoesNaoLidas } from "@/features/plataforma/hooks/useNotificacoes";
import type { TipoNotificacao } from "@/features/plataforma/types/plataforma";

const COR_TIPO: Record<TipoNotificacao, string> = {
  NovoCadastro: "bg-blue-500",
  PlanoAssinado: "bg-emerald-500",
  PlanoRenovado: "bg-emerald-500",
  PlanoTrocado: "bg-amber-500",
  TesteVencido: "bg-red-500",
  PlanoVencido: "bg-red-500",
};

export function SinoNotificacoes() {
  const [aberto, setAberto] = useState(false);
  const [soNaoLidas, setSoNaoLidas] = useState(false);
  const { data: naoLidas = 0 } = useNotificacoesNaoLidas(true);
  const { data: pagina, isLoading } = useNotificacoes(aberto, soNaoLidas);
  const marcar = useMarcarNotificacoes();

  return (
    <Sheet open={aberto} onOpenChange={setAberto}>
      <SheetTrigger asChild>
        <Button variant="ghost" size="icon" className="relative" aria-label={`Notificações (${naoLidas} não lidas)`}>
          <Bell className="size-5" />
          {naoLidas > 0 && (
            <span className="absolute -right-0.5 -top-0.5 flex min-w-5 items-center justify-center rounded-full bg-red-500 px-1 text-[10px] font-semibold text-white">
              {naoLidas > 99 ? "99+" : naoLidas}
            </span>
          )}
        </Button>
      </SheetTrigger>

      <SheetContent side="right" className="flex w-full flex-col gap-0 sm:max-w-md">
        <SheetHeader className="border-b">
          <SheetTitle>Notificações</SheetTitle>
          <SheetDescription>O que os clientes fizeram na plataforma.</SheetDescription>
          <div className="flex items-center justify-between pt-2">
            <div className="flex gap-1">
              <Button size="sm" variant={soNaoLidas ? "ghost" : "secondary"} onClick={() => setSoNaoLidas(false)}>
                Todas
              </Button>
              <Button size="sm" variant={soNaoLidas ? "secondary" : "ghost"} onClick={() => setSoNaoLidas(true)}>
                Não lidas ({naoLidas})
              </Button>
            </div>
            <Button size="sm" variant="outline" disabled={naoLidas === 0 || marcar.isPending} onClick={() => marcar.mutate(undefined)}>
              <CheckCheck className="size-4" />
              Marcar todas
            </Button>
          </div>
        </SheetHeader>

        <div className="flex-1 overflow-y-auto">
          {isLoading && (
            <div className="space-y-3 p-4">
              <Skeleton className="h-16 w-full" />
              <Skeleton className="h-16 w-full" />
              <Skeleton className="h-16 w-full" />
            </div>
          )}

          {!isLoading && pagina?.itens.length === 0 && (
            <p className="p-6 text-center text-sm text-muted-foreground">
              {soNaoLidas ? "Nada novo por aqui." : "Nenhuma notificação ainda."}
            </p>
          )}

          <ul className="divide-y">
            {pagina?.itens.map((notificacao) => (
              <li key={notificacao.id}>
                <button
                  type="button"
                  onClick={() => !notificacao.lida && marcar.mutate(notificacao.id)}
                  className={cn(
                    "flex w-full gap-3 px-4 py-3 text-left transition-colors hover:bg-muted/50",
                    !notificacao.lida && "bg-primary/5",
                  )}
                >
                  <span className={cn("mt-1.5 size-2 shrink-0 rounded-full", COR_TIPO[notificacao.tipo] ?? "bg-muted-foreground")} />
                  <span className="min-w-0 flex-1">
                    <span className={cn("block text-sm", !notificacao.lida && "font-semibold")}>{notificacao.titulo}</span>
                    <span className="block text-sm text-muted-foreground">{notificacao.mensagem}</span>
                    <span className="mt-1 block text-xs text-muted-foreground">{formatarDataHora(notificacao.ocorridoEm)}</span>
                  </span>
                </button>
              </li>
            ))}
          </ul>
        </div>

        <div className="border-t p-3 text-center">
          <Link href="/admin-plataforma" className="text-sm text-primary hover:underline" onClick={() => setAberto(false)}>
            Ir para o painel da plataforma
          </Link>
        </div>
      </SheetContent>
    </Sheet>
  );
}
