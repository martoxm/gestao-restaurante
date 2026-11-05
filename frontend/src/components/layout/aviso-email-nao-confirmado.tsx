"use client";

import { MailWarning } from "lucide-react";
import { useMutation } from "@tanstack/react-query";
import { toast } from "sonner";
import { reenviarConfirmacaoEmail } from "@/features/auth/services/auth";
import { useUsuarioAtual } from "@/features/auth/hooks/useUsuarioAtual";
import { obterMensagemErro } from "@/lib/api-errors";
import { Button } from "@/components/ui/button";

export function AvisoEmailNaoConfirmado() {
  const { emailConfirmado } = useUsuarioAtual();
  const reenviar = useMutation({
    mutationFn: reenviarConfirmacaoEmail,
    onSuccess: () => toast.success("Enviamos o link de novo. Confira sua caixa de entrada."),
    onError: (error) => toast.error(obterMensagemErro(error, "Não foi possível reenviar o e-mail.")),
  });

  if (emailConfirmado) return null;

  return (
    <div className="mb-4 flex flex-col gap-2 rounded-lg border border-amber-500/40 bg-amber-500/10 p-3 text-sm sm:flex-row sm:items-center sm:justify-between">
      <p className="flex items-start gap-2">
        <MailWarning className="mt-0.5 size-4 shrink-0" />
        Confirme seu e-mail pelo link que enviamos quando você criou a conta.
      </p>
      <Button size="sm" variant="outline" onClick={() => reenviar.mutate()} disabled={reenviar.isPending}>
        {reenviar.isPending ? "Enviando..." : "Reenviar e-mail"}
      </Button>
    </div>
  );
}
