"use client";

import { Suspense, useEffect, useState } from "react";
import { useSearchParams } from "next/navigation";
import { Check, CheckCircle2, Info, XCircle } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { formatarData } from "@/lib/datas";
import { renovarSessao } from "@/lib/axios";
import { obterRefreshToken } from "@/lib/sessao";
import { useUsuarioAtual } from "@/features/auth/hooks/useUsuarioAtual";
import { useRestaurante } from "@/features/restaurantes/hooks/useRestaurante";
import { useIniciarAssinatura, usePlanos } from "@/features/assinatura/hooks/useAssinatura";
import type { PlanoPago } from "@/features/assinatura/types/assinatura";

const BENEFICIOS: Record<PlanoPago, string[]> = {
  Basico: ["Até 5 usuários", "Estoque, vendas e pagamentos", "Avisos de estoque baixo por e-mail"],
  Pro: ["Usuários ilimitados", "Tudo do Básico", "Ideal para equipes maiores"],
};

const NOMES_PLANO: Record<string, string> = { Teste: "Teste grátis", Basico: "Básico", Pro: "Pro" };

function comFuso(data: string) {
  return data.endsWith("Z") ? data : `${data}Z`;
}

function formatarPreco(valor: number) {
  return valor.toLocaleString("pt-BR", { style: "currency", currency: "BRL" });
}

export function AssinaturaPage() {
  return (
    <Suspense>
      <Assinatura />
    </Suspense>
  );
}

function Assinatura() {
  const status = useSearchParams().get("status");
  const simulado = useSearchParams().get("simulado") === "1";
  const { restauranteId } = useUsuarioAtual();

  const { data: restaurante } = useRestaurante(restauranteId ?? undefined, {
    refetchInterval: status === "sucesso" ? 3000 : false,
  });
  const [agora] = useState(() => Date.now());
  const planoPagoValendo =
    !!restaurante?.planoValidoAte && new Date(comFuso(restaurante.planoValidoAte)).getTime() > agora;

  useEffect(() => {
    if (status !== "sucesso" || !planoPagoValendo) return;
    (async () => {
      if (obterRefreshToken()) await renovarSessao().catch(() => undefined);
      window.location.replace("/assinatura?status=liberado");
    })();
  }, [status, planoPagoValendo]);
  const { data: planos, isLoading, isError } = usePlanos();
  const iniciar = useIniciarAssinatura();

  return (
    <div className="mx-auto flex max-w-4xl flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold">Assinatura</h1>
        <p className="text-sm text-muted-foreground">
          Cada pagamento vale 30 dias. É tudo em modo de teste: nenhum dinheiro de verdade é cobrado.
        </p>
      </div>

      {status === "sucesso" && (
        <div role="status" className="flex items-start gap-3 rounded-lg border border-emerald-500/40 bg-emerald-500/10 p-3 text-sm text-emerald-700 dark:text-emerald-400">
          <CheckCircle2 className="mt-0.5 size-4 shrink-0" />
          <p>
            {simulado ? "Pagamento simulado (gateway falso, sem Stripe). " : "Pagamento recebido! "}
            Aguardando a confirmação para liberar o plano...
          </p>
        </div>
      )}
      {status === "liberado" && restaurante?.planoValidoAte && (
        <div role="status" className="flex items-start gap-3 rounded-lg border border-emerald-500/40 bg-emerald-500/10 p-3 text-sm text-emerald-700 dark:text-emerald-400">
          <CheckCircle2 className="mt-0.5 size-4 shrink-0" />
          <p>Plano liberado até {formatarData(restaurante.planoValidoAte)}. Pode voltar a usar tudo normalmente.</p>
        </div>
      )}
      {status === "cancelado" && (
        <div role="status" className="flex items-start gap-3 rounded-lg border p-3 text-sm text-muted-foreground">
          <XCircle className="mt-0.5 size-4 shrink-0" />
          <p>Você saiu da página de pagamento sem pagar. Nada foi cobrado.</p>
        </div>
      )}

      {restaurante && (
        <Card>
          <CardHeader>
            <CardDescription>Plano atual</CardDescription>
            <CardTitle className="flex items-center gap-2">
              {NOMES_PLANO[restaurante.plano] ?? restaurante.plano}
              {!restaurante.ativo && <Badge variant="destructive">Desativado</Badge>}
            </CardTitle>
          </CardHeader>
          {restaurante.plano === "Teste" && restaurante.testeTerminaEm && (
            <CardContent className="text-sm text-muted-foreground">
              O teste termina em {formatarData(restaurante.testeTerminaEm)}.
            </CardContent>
          )}
          {restaurante.plano !== "Teste" && (
            <CardContent className="text-sm text-muted-foreground">
              {restaurante.planoValidoAte
                ? `${planoPagoValendo ? "Válido até" : "Venceu em"} ${formatarData(restaurante.planoValidoAte)}. Pagar de novo soma mais 30 dias.`
                : "Plano sem data de vencimento."}
            </CardContent>
          )}
        </Card>
      )}

      {isError && (
        <p className="flex items-center gap-2 text-sm text-destructive">
          <Info className="size-4" /> Não foi possível carregar os planos.
        </p>
      )}

      <div className="grid gap-4 sm:grid-cols-2">
        {isLoading &&
          [0, 1].map((i) => <Skeleton key={i} className="h-64 rounded-xl" />)}

        {planos?.map((plano) => (
          <Card key={plano.plano} className="flex flex-col">
            <CardHeader>
              <CardTitle>{plano.nome}</CardTitle>
              <CardDescription>
                <span className="text-2xl font-semibold text-foreground">{formatarPreco(plano.preco)}</span> / {plano.dias} dias
              </CardDescription>
            </CardHeader>
            <CardContent className="flex-1">
              <ul className="flex flex-col gap-2 text-sm">
                {BENEFICIOS[plano.plano].map((beneficio) => (
                  <li key={beneficio} className="flex items-center gap-2">
                    <Check className="size-4 text-emerald-600" /> {beneficio}
                  </li>
                ))}
              </ul>
            </CardContent>
            <CardFooter>
              <Button
                className="w-full"
                disabled={iniciar.isPending}
                onClick={() => iniciar.mutate(plano.plano)}
              >
                {iniciar.isPending && iniciar.variables === plano.plano ? "Abrindo pagamento..." : `Assinar ${plano.nome}`}
              </Button>
            </CardFooter>
          </Card>
        ))}
      </div>
    </div>
  );
}
