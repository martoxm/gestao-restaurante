"use client";

import { Suspense } from "react";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useForm } from "react-hook-form";
import { useMutation } from "@tanstack/react-query";
import { toast } from "sonner";
import { redefinirSenha } from "@/features/auth/services/auth";
import { aplicarErrosDaApi } from "@/lib/api-errors";
import { limparSessao } from "@/lib/sessao";
import { FormRootError } from "@/components/form-root-error";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";

export function RedefinirSenhaPage() {
  return (
    <div className="flex flex-1 items-center justify-center bg-zinc-50 p-4 dark:bg-black">
      <Card className="w-full max-w-sm">
        <Suspense fallback={<CardContent className="pt-6 text-sm">Carregando...</CardContent>}>
          <Conteudo />
        </Suspense>
      </Card>
    </div>
  );
}

interface DadosFormulario {
  novaSenha: string;
  confirmacao: string;
}

function Conteudo() {
  const params = useSearchParams();
  const email = params.get("email");
  const token = params.get("token");
  const router = useRouter();
  const form = useForm<DadosFormulario>({ defaultValues: { novaSenha: "", confirmacao: "" } });
  const mutation = useMutation({ mutationFn: redefinirSenha });

  if (!email || !token) {
    return (
      <CardHeader>
        <CardTitle>Link incompleto</CardTitle>
        <CardDescription>
          Abra o link exatamente como chegou no e-mail, ou <Link href="/esqueci-senha" className="underline">peça um novo</Link>.
        </CardDescription>
      </CardHeader>
    );
  }

  function onSubmit({ novaSenha }: DadosFormulario) {
    mutation.mutate(
      { email: email!, token: token!, novaSenha },
      {
        onSuccess: () => {
          limparSessao();
          toast.success("Senha alterada. Entre com a senha nova.");
          router.push("/login");
        },
        onError: (error) => aplicarErrosDaApi(error, form),
      }
    );
  }

  return (
    <>
      <CardHeader>
        <CardTitle>Criar senha nova</CardTitle>
        <CardDescription>Para {email}. Depois de trocar, todas as sessões abertas serão encerradas.</CardDescription>
      </CardHeader>
      <CardContent>
        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="flex flex-col gap-4">
            <FormField
              control={form.control}
              name="novaSenha"
              rules={{
                required: "A senha nova é obrigatória.",
                minLength: { value: 6, message: "A senha precisa ter pelo menos 6 caracteres." },
              }}
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Senha nova</FormLabel>
                  <FormControl>
                    <Input type="password" autoFocus autoComplete="new-password" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="confirmacao"
              rules={{
                validate: (valor, dados) => valor === dados.novaSenha || "As senhas não são iguais.",
              }}
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Repita a senha nova</FormLabel>
                  <FormControl>
                    <Input type="password" autoComplete="new-password" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormRootError />
            <Button type="submit" disabled={mutation.isPending}>
              {mutation.isPending ? "Salvando..." : "Salvar senha nova"}
            </Button>
          </form>
        </Form>
      </CardContent>
    </>
  );
}
