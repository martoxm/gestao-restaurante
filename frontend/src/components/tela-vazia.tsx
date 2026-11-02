import Link from "next/link";
import type { LucideIcon } from "lucide-react";
import { Button } from "@/components/ui/button";

export function TelaVazia({
  icone: Icone,
  titulo,
  descricao,
  acao,
}: {
  icone: LucideIcon;
  titulo: string;
  descricao: string;
  acao?: { href: string; rotulo: string };
}) {
  return (
    <div className="flex flex-col items-center gap-3 rounded-lg border border-dashed px-6 py-12 text-center">
      <div className="rounded-full bg-muted p-3">
        <Icone className="size-6 text-muted-foreground" />
      </div>
      <div className="flex flex-col gap-1">
        <p className="font-medium">{titulo}</p>
        <p className="max-w-sm text-sm text-muted-foreground">{descricao}</p>
      </div>
      {acao && (
        <Button asChild>
          <Link href={acao.href}>{acao.rotulo}</Link>
        </Button>
      )}
    </div>
  );
}
