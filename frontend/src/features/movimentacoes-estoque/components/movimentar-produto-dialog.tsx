"use client";

import { useState } from "react";
import { useForm } from "react-hook-form";
import { ArrowDownUp } from "lucide-react";
import { toast } from "sonner";
import { aplicarErrosDaApi } from "@/lib/api-errors";
import { FormRootError } from "@/components/form-root-error";
import { useCriarMovimentacaoEstoque } from "@/features/movimentacoes-estoque/hooks/useCriarMovimentacaoEstoque";
import type { TipoMovimentacaoEstoque } from "@/features/movimentacoes-estoque/types/movimentacao-estoque";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";

interface MovimentarProdutoFormValues {
  tipo: TipoMovimentacaoEstoque;
  quantidade: string;
  observacoes: string;
}

export function MovimentarProdutoDialog({
  produtoId,
  nomeProduto,
  quantidadeEmEstoque,
}: {
  produtoId: string;
  nomeProduto: string;
  quantidadeEmEstoque: number;
}) {
  const [aberto, setAberto] = useState(false);
  const mutation = useCriarMovimentacaoEstoque({ redirecionar: false });
  const form = useForm<MovimentarProdutoFormValues>({
    defaultValues: { tipo: "Entrada", quantidade: "", observacoes: "" },
  });

  function trocarAberto(novo: boolean) {
    if (novo) form.reset();
    setAberto(novo);
  }

  function onSubmit(dados: MovimentarProdutoFormValues) {
    mutation.mutate(
      {
        produtoId,
        tipo: dados.tipo,
        quantidade: Number(dados.quantidade),
        observacoes: dados.observacoes || undefined,
      },
      {
        onSuccess: () => {
          toast.success(
            `${dados.tipo === "Entrada" ? "Entrada" : "Saída"} de ${dados.quantidade} em "${nomeProduto}" registrada.`
          );
          setAberto(false);
        },
        onError: (error) => aplicarErrosDaApi(error, form),
      }
    );
  }

  return (
    <Dialog open={aberto} onOpenChange={trocarAberto}>
      <DialogTrigger asChild>
        <Button variant="outline" size="sm">
          <ArrowDownUp className="size-3.5" />
          Movimentar
        </Button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Movimentar &quot;{nomeProduto}&quot;</DialogTitle>
          <DialogDescription>Estoque atual: {quantidadeEmEstoque}</DialogDescription>
        </DialogHeader>
        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="flex flex-col gap-4">
            <FormField
              control={form.control}
              name="tipo"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Tipo</FormLabel>
                  <Select value={field.value} onValueChange={field.onChange}>
                    <FormControl>
                      <SelectTrigger className="w-full">
                        <SelectValue />
                      </SelectTrigger>
                    </FormControl>
                    <SelectContent>
                      <SelectItem value="Entrada">Entrada</SelectItem>
                      <SelectItem value="Saida">Saída</SelectItem>
                    </SelectContent>
                  </Select>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="quantidade"
              rules={{
                required: "Informe a quantidade.",
                validate: (v) =>
                  (Number.isInteger(Number(v)) && Number(v) > 0) ||
                  "A quantidade tem que ser um número inteiro maior que zero.",
              }}
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Quantidade</FormLabel>
                  <FormControl>
                    <Input type="number" min={1} step={1} autoFocus {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="observacoes"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Observações (opcional)</FormLabel>
                  <FormControl>
                    <Input {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormRootError />
            <DialogFooter>
              <Button type="submit" disabled={mutation.isPending}>
                {mutation.isPending ? "Salvando..." : "Registrar"}
              </Button>
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  );
}
