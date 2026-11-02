"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";

export function useAvisoSemPermissao(rotaAtual: string, mensagem: string) {
  const router = useRouter();

  useEffect(() => {
    const parametros = new URLSearchParams(window.location.search);
    if (parametros.get("aviso") !== "sem-permissao") return;

    toast.error(mensagem, { id: "aviso-sem-permissao" });
    router.replace(rotaAtual);
  }, [router, rotaAtual, mensagem]);
}
