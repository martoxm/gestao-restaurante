"use client";

import { Suspense, useEffect, useRef, useState } from "react";
import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { confirmarEmail } from "@/features/auth/services/auth";
import { renovarSessao } from "@/lib/axios";
import { obterMensagemErro } from "@/lib/api-errors";
import { obterRefreshToken } from "@/lib/sessao";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";

export function ConfirmarEmailPage() {
  return (
    <div className="flex flex-1 items-center justify-center bg-zinc-50 p-4 dark:bg-black">
      <Card className="w-full max-w-sm">
        <Suspense fallback={<CardContent className="pt-6 text-sm">Confirmando...</CardContent>}>
          <Conteudo />
        </Suspense>
      </Card>
    </div>
  );
}

type Estado = { tipo: "carregando" } | { tipo: "ok" } | { tipo: "erro"; mensagem: string };

function Conteudo() {
  const params = useSearchParams();
  const usuarioId = params.get("usuarioId");
  const token = params.get("token");
  const [estado, setEstado] = useState<Estado>({ tipo: "carregando" });
  const jaChamou = useRef(false);

  useEffect(() => {
    if (jaChamou.current || !usuarioId || !token) return;
    jaChamou.current = true;

    confirmarEmail(usuarioId, token)
      .then(async () => {
        if (obterRefreshToken()) await renovarSessao().catch(() => undefined);
        setEstado({ tipo: "ok" });
      })
      .catch((error) => setEstado({ tipo: "erro", mensagem: obterMensagemErro(error) }));
  }, [usuarioId, token]);

  if (!usuarioId || !token) {
    return (
      <CardHeader>
        <CardTitle>Link incompleto</CardTitle>
        <CardDescription>Abra o link exatamente como chegou no e-mail.</CardDescription>
      </CardHeader>
    );
  }

  return (
    <>
      <CardHeader>
        <CardTitle>
          {estado.tipo === "carregando" && "Confirmando seu e-mail..."}
          {estado.tipo === "ok" && "E-mail confirmado!"}
          {estado.tipo === "erro" && "Não deu para confirmar"}
        </CardTitle>
        {estado.tipo === "erro" && <CardDescription>{estado.mensagem}</CardDescription>}
      </CardHeader>
      {estado.tipo !== "carregando" && (
        <CardContent>
          <Link href="/" className="text-sm underline">
            Ir para o sistema
          </Link>
        </CardContent>
      )}
    </>
  );
}
