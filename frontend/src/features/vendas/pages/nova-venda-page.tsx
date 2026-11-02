"use client";

import Link from "next/link";
import { useEscParaVoltar } from "@/lib/use-esc-para-voltar";
import { useFieldArray, useForm, useWatch } from "react-hook-form";
import { Trash2 } from "lucide-react";
import { useProdutos } from "@/features/produtos/hooks/useProdutos";
import { useCriarVenda } from "@/features/vendas/hooks/useCriarVenda";
import { CARTOES_TESTE, formatarMoeda } from "@/features/vendas/types/venda";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
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
import { FormRootError } from "@/components/form-root-error";
import { aplicarErrosDaApi } from "@/lib/api-errors";

type VendaFormValues = {
  itens: { produtoId: string; quantidade: string }[];
  tokenCartao: string;
};

export function NovaVendaPage() {
  useEscParaVoltar("/vendas");
  const form = useForm<VendaFormValues>({
    defaultValues: {
      itens: [{ produtoId: "", quantidade: "1" }],
      tokenCartao: "tok_aprovado",
    },
  });

  const { fields, append, remove } = useFieldArray({ control: form.control, name: "itens" });

  const { data: produtos, isLoading: isLoadingProdutos } = useProdutos({ tamanhoPagina: 100 });
  const criarVendaMutation = useCriarVenda();

  const itens = useWatch({ control: form.control, name: "itens" });
  const totalPrevisto = itens.reduce((soma, item) => {
    const produto = produtos?.itens.find((p) => p.id === item.produtoId);
    return soma + (produto ? produto.preco * Number(item.quantidade || 0) : 0);
  }, 0);

  function onSubmit(data: VendaFormValues) {
    criarVendaMutation.mutate(
      {
        itens: data.itens.map((item) => ({
          produtoId: item.produtoId,
          quantidade: Number(item.quantidade),
        })),
        tokenCartao: data.tokenCartao,
      },
      { onError: (error) => aplicarErrosDaApi(error, form) }
    );
  }

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center justify-between">
        <h1 className="font-heading text-lg font-semibold">Nova venda</h1>
        <Button variant="outline" asChild>
          <Link href="/vendas">Voltar</Link>
        </Button>
      </div>

      <Card className="max-w-2xl">
        <CardHeader>
          <CardTitle>Itens da venda</CardTitle>
        </CardHeader>
        <CardContent>
          <Form {...form}>
            <form onSubmit={form.handleSubmit(onSubmit)} className="flex flex-col gap-4">
              {fields.map((campo, indice) => (
                <div key={campo.id} className="flex items-start gap-2">
                  <FormField
                    control={form.control}
                    name={`itens.${indice}.produtoId`}
                    rules={{
                      required: "Selecione um produto.",
                      validate: (valor) =>
                        form.getValues("itens").filter((i) => i.produtoId === valor).length === 1 ||
                        "Esse produto já está na venda.",
                    }}
                    render={({ field }) => (
                      <FormItem className="min-w-0 flex-1">
                        {indice === 0 && <FormLabel>Produto</FormLabel>}
                        <Select value={field.value} onValueChange={field.onChange} disabled={isLoadingProdutos}>
                          <FormControl>
                            <SelectTrigger className="w-full">
                              <SelectValue placeholder="Selecione um produto" />
                            </SelectTrigger>
                          </FormControl>
                          <SelectContent>
                            {produtos?.itens.map((produto) => (
                              <SelectItem
                                key={produto.id}
                                value={produto.id}
                                disabled={produto.quantidadeEmEstoque === 0}
                              >
                                {produto.nome} — {formatarMoeda(produto.preco)} (estoque: {produto.quantidadeEmEstoque})
                              </SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
                        <FormMessage />
                      </FormItem>
                    )}
                  />

                  <FormField
                    control={form.control}
                    name={`itens.${indice}.quantidade`}
                    rules={{
                      required: "Informe a quantidade.",
                      min: { value: 1, message: "Mínimo 1." },
                      validate: (valor) => {
                        const produto = produtos?.itens.find(
                          (p) => p.id === form.getValues(`itens.${indice}.produtoId`)
                        );
                        if (!produto) return true;
                        if (produto.quantidadeEmEstoque === 0) return "Produto esgotado.";
                        return (
                          Number(valor) <= produto.quantidadeEmEstoque ||
                          `Só há ${produto.quantidadeEmEstoque} em estoque.`
                        );
                      },
                    }}
                    render={({ field }) => (
                      <FormItem className="w-32">
                        {indice === 0 && <FormLabel>Qtd.</FormLabel>}
                        <FormControl>
                          <Input type="number" step="1" min="1" {...field} />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />

                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    className={indice === 0 ? "mt-6" : undefined}
                    disabled={fields.length === 1}
                    onClick={() => remove(indice)}
                    aria-label="Remover item"
                  >
                    <Trash2 />
                  </Button>
                </div>
              ))}

              <Button
                type="button"
                variant="outline"
                className="self-start"
                onClick={() => append({ produtoId: "", quantidade: "1" })}
              >
                Adicionar item
              </Button>

              <FormField
                control={form.control}
                name="tokenCartao"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Cartão de teste</FormLabel>
                    <Select value={field.value} onValueChange={field.onChange}>
                      <FormControl>
                        <SelectTrigger className="w-full">
                          <SelectValue />
                        </SelectTrigger>
                      </FormControl>
                      <SelectContent>
                        {CARTOES_TESTE.map((cartao) => (
                          <SelectItem key={cartao.token} value={cartao.token}>
                            {cartao.rotulo}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <p className="text-sm text-muted-foreground">
                Total previsto:{" "}
                <span className="font-medium text-foreground">{formatarMoeda(totalPrevisto)}</span>
              </p>

              <FormRootError />

              <Button type="submit" disabled={criarVendaMutation.isPending} className="mt-2">
                {criarVendaMutation.isPending ? "Registrando..." : "Registrar venda"}
              </Button>
            </form>
          </Form>
        </CardContent>
      </Card>
    </div>
  );
}
