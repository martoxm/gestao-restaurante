import { FlaskConical } from "lucide-react";

export function AvisoProjetoEstudo() {
  return (
    <div className="flex items-center justify-center gap-2 border-b bg-amber-100 px-4 py-1.5 text-center text-xs text-amber-900 dark:bg-amber-950 dark:text-amber-200">
      <FlaskConical className="size-3.5 shrink-0" aria-hidden />
      <span>
        Projeto de estudo: os dados podem ser apagados a qualquer momento.
        Pagamentos em modo teste, use o cartão{" "}
        <strong className="font-mono">4242 4242 4242 4242</strong> (nunca um
        cartão de verdade).
      </span>
    </div>
  );
}
