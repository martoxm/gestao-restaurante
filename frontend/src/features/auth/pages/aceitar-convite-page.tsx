"use client";

import { Suspense } from "react";
import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { useForm } from "react-hook-form";
import { useAceitarConvite, useConvitePublico } from "@/features/auth/hooks/useConvitePublico";
import type { ConvitePublico } from "@/features/auth/types/auth";
import { aplicarErrosDaApi } from "@/lib/api-errors";
import { formatarData } from "@/lib/datas";
import { FormRootError } from "@/components/form-root-error";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";

export function AceitarConvitePage() {
  return (
    <div className="flex flex-1 items-center justify-center bg-zinc-50 p-4 dark:bg-black">
      <Card className="w-full max-w-sm">
        <Suspense fallback={<CardContent className="pt-6 text-sm">Carregando convite...</CardContent>}>
          <ConteudoConvite />
        </Suspense>
      </Card>
    </div>
  );
}

function ConteudoConvite() {
  const token = useSearchParams().get("token");
  const { data: convite, isLoading, isError } = useConvitePublico(token);

  if (!token || isError) {
    return (
      <>
        <CardHeader>
          <CardTitle>Convite inválido</CardTitle>
          <CardDescription>
            Este link não existe, já foi usado, foi cancelado ou venceu. Peça um convite novo para o
            administrador do restaurante.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <Link href="/login" className="text-sm underline">
            Ir para o login
          </Link>
        </CardContent>
      </>
    );
  }

  if (isLoading || !convite) {
    return <CardContent className="pt-6 text-sm">Carregando convite...</CardContent>;
  }

  return <FormularioAceite token={token} convite={convite} />;
}

interface DadosFormulario {
  nome: string;
  senha: string;
}

function FormularioAceite({ token, convite }: { token: string; convite: ConvitePublico }) {
  const form = useForm<DadosFormulario>({ defaultValues: { nome: "", senha: "" } });
  const aceitarMutation = useAceitarConvite();

  function onSubmit(dados: DadosFormulario) {
    aceitarMutation.mutate({ token, ...dados }, { onError: (error) => aplicarErrosDaApi(error, form) });
  }

  return (
    <>
      <CardHeader>
        <CardTitle>Entrar em {convite.nomeRestaurante}</CardTitle>
        <CardDescription>
          Você foi convidado como {convite.role === "Admin" ? "Admin" : "Funcionário"}. Crie sua senha para
          entrar. O convite vale até {formatarData(convite.expiraEm)}.
        </CardDescription>
      </CardHeader>
      <CardContent>
        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="flex flex-col gap-4">
            <FormItem>
              <FormLabel>E-mail</FormLabel>
              <Input value={convite.email} disabled readOnly />
            </FormItem>

            <FormField
              control={form.control}
              name="nome"
              rules={{ required: "O nome é obrigatório." }}
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Seu nome</FormLabel>
                  <FormControl>
                    <Input autoFocus autoComplete="name" placeholder="Seu nome completo" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="senha"
              rules={{
                required: "A senha é obrigatória.",
                minLength: { value: 6, message: "A senha precisa ter pelo menos 6 caracteres." },
              }}
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Senha</FormLabel>
                  <FormControl>
                    <Input type="password" autoComplete="new-password" placeholder="••••••••" {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormRootError />

            <Button type="submit" disabled={aceitarMutation.isPending} className="mt-2">
              {aceitarMutation.isPending ? "Entrando..." : "Aceitar convite e entrar"}
            </Button>
          </form>
        </Form>
      </CardContent>
    </>
  );
}
