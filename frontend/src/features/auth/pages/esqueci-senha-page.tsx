"use client";

import { useState } from "react";
import Link from "next/link";
import { useForm } from "react-hook-form";
import { useMutation } from "@tanstack/react-query";
import { esqueciSenha } from "@/features/auth/services/auth";
import { aplicarErrosDaApi } from "@/lib/api-errors";
import { FormRootError } from "@/components/form-root-error";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";

export function EsqueciSenhaPage() {
  const [enviado, setEnviado] = useState(false);
  const form = useForm<{ email: string }>({ defaultValues: { email: "" } });
  const mutation = useMutation({ mutationFn: esqueciSenha });

  function onSubmit({ email }: { email: string }) {
    mutation.mutate(email, {
      onSuccess: () => setEnviado(true),
      onError: (error) => aplicarErrosDaApi(error, form),
    });
  }

  return (
    <div className="flex flex-1 items-center justify-center bg-zinc-50 p-4 dark:bg-black">
      <Card className="w-full max-w-sm">
        <CardHeader>
          <CardTitle>Esqueci a senha</CardTitle>
          <CardDescription>
            {enviado
              ? "Se existir uma conta com esse e-mail, enviamos um link para criar uma senha nova. Ele vale por 1 dia."
              : "Digite o e-mail da sua conta e enviaremos um link para você criar uma senha nova."}
          </CardDescription>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          {!enviado && (
            <Form {...form}>
              <form onSubmit={form.handleSubmit(onSubmit)} className="flex flex-col gap-4">
                <FormField
                  control={form.control}
                  name="email"
                  rules={{
                    required: "O e-mail é obrigatório.",
                    pattern: { value: /^[^\s@]+@[^\s@]+\.[^\s@]+$/, message: "O e-mail informado não é válido." },
                  }}
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>E-mail</FormLabel>
                      <FormControl>
                        <Input type="email" autoFocus autoComplete="email" placeholder="seu@email.com" {...field} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                <FormRootError />
                <Button type="submit" disabled={mutation.isPending}>
                  {mutation.isPending ? "Enviando..." : "Enviar link"}
                </Button>
              </form>
            </Form>
          )}
          <Link href="/login" className="text-center text-sm underline">
            Voltar para o login
          </Link>
        </CardContent>
      </Card>
    </div>
  );
}
