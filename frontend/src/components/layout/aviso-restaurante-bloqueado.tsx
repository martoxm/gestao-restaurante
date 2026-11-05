"use client";

import Link from "next/link";
import { Lock } from "lucide-react";
import { useUsuarioAtual } from "@/features/auth/hooks/useUsuarioAtual";

export function AvisoRestauranteBloqueado() {
  const { restauranteLiberado, isAdmin } = useUsuarioAtual();

  if (restauranteLiberado) return null;

  return (
    <div
      role="alert"
      className="mb-4 flex items-start gap-3 rounded-lg border border-destructive/40 bg-destructive/10 p-3 text-sm text-destructive"
    >
      <Lock className="mt-0.5 size-4 shrink-0" />
      <p>
        O período de teste do restaurante terminou (ou o acesso foi suspenso). Você ainda pode consultar
        tudo, mas não dá para criar, alterar ou excluir nada até o plano ser renovado.
        {isAdmin && (
          <>
            {" "}
            <Link href="/assinatura" className="font-semibold underline underline-offset-2">
              Escolher um plano
            </Link>
          </>
        )}
      </p>
    </div>
  );
}
