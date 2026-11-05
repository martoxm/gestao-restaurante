"use client";

import { useForm } from "react-hook-form";
import { Mail } from "lucide-react";
import { toast } from "sonner";
import { aplicarErrosDaApi, obterMensagemErro } from "@/lib/api-errors";
import { formatarData } from "@/lib/datas";
import { useCancelarConvite, useConvites, useCriarConvite } from "@/features/usuarios/hooks/useConvites";
import type { ConviteItemLista, CriarConviteRequest, StatusConvite } from "@/features/usuarios/types/convite";
import type { RoleUsuario } from "@/features/usuarios/types/usuario";
import { ConfirmarExclusao } from "@/components/confirmar-exclusao";
import { FormRootError } from "@/components/form-root-error";
import { TelaVazia } from "@/components/tela-vazia";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";

const VARIANTE_STATUS: Record<StatusConvite, "default" | "secondary" | "destructive" | "outline"> = {
  Pendente: "default",
  Aceito: "secondary",
  Cancelado: "outline",
  Expirado: "destructive",
};

export function ConvitesSection() {
  const { data, isLoading, isError } = useConvites();
  const criarMutation = useCriarConvite();
  const cancelarMutation = useCancelarConvite();

  const form = useForm<CriarConviteRequest>({
    defaultValues: { email: "", role: "Funcionario" },
  });

  function onSubmit(dados: CriarConviteRequest) {
    criarMutation.mutate(dados, {
      onSuccess: () => form.reset({ email: "", role: dados.role }),
      onError: (error) => aplicarErrosDaApi(error, form),
    });
  }

  function cancelar(convite: ConviteItemLista) {
    cancelarMutation.mutate(convite.id, {
      onError: (error) => toast.error(obterMensagemErro(error, "Não foi possível cancelar o convite.")),
    });
  }

  return (
    <section className="flex flex-col gap-4">
      <div>
        <h2 className="font-heading text-base font-semibold">Convites</h2>
        <p className="text-sm text-muted-foreground">
          Convide alguém pelo e-mail. A pessoa recebe um link (válido por 7 dias, uso único) para criar a
          senha e entrar direto neste restaurante. Convidar de novo o mesmo e-mail cancela o convite anterior.
        </p>
      </div>

      <Form {...form}>
        <form onSubmit={form.handleSubmit(onSubmit)} className="flex flex-col gap-3 sm:flex-row sm:items-end">
          <FormField
            control={form.control}
            name="email"
            rules={{
              required: "O e-mail é obrigatório.",
              pattern: { value: /^[^\s@]+@[^\s@]+\.[^\s@]+$/, message: "O e-mail informado não é válido." },
            }}
            render={({ field }) => (
              <FormItem className="flex-1">
                <FormLabel>E-mail</FormLabel>
                <FormControl>
                  <Input type="email" placeholder="pessoa@email.com" {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
          <FormField
            control={form.control}
            name="role"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Role</FormLabel>
                <Select value={field.value} onValueChange={(valor) => field.onChange(valor as RoleUsuario)}>
                  <FormControl>
                    <SelectTrigger className="w-40">
                      <SelectValue />
                    </SelectTrigger>
                  </FormControl>
                  <SelectContent>
                    <SelectItem value="Funcionario">Funcionário</SelectItem>
                    <SelectItem value="Admin">Admin</SelectItem>
                  </SelectContent>
                </Select>
                <FormMessage />
              </FormItem>
            )}
          />
          <Button type="submit" disabled={criarMutation.isPending}>
            {criarMutation.isPending ? "Enviando..." : "Enviar convite"}
          </Button>
        </form>
        <FormRootError />
      </Form>

      {isLoading && <Skeleton className="h-10 w-full" />}

      {isError && <p className="text-sm text-destructive">Não foi possível carregar os convites.</p>}

      {data && data.length === 0 && (
        <TelaVazia icone={Mail} titulo="Nenhum convite" descricao="Os convites enviados aparecem aqui." />
      )}

      {data && data.length > 0 && (
        <ul className="flex flex-col gap-2">
          {data.map((convite) => (
            <li
              key={convite.id}
              className="flex flex-col gap-2 rounded-lg border p-3 sm:flex-row sm:items-center sm:justify-between"
            >
              <div className="flex flex-col gap-1">
                <p className="font-medium">{convite.email}</p>
                <p className="text-xs text-muted-foreground">
                  {convite.role === "Admin" ? "Admin" : "Funcionário"} · enviado em {formatarData(convite.criadoEm)}
                  {convite.status === "Pendente" && ` · vence em ${formatarData(convite.expiraEm)}`}
                </p>
              </div>
              <div className="flex items-center gap-2">
                <Badge variant={VARIANTE_STATUS[convite.status]}>{convite.status}</Badge>
                {convite.status === "Pendente" && (
                  <ConfirmarExclusao
                    rotulo="Cancelar"
                    titulo="Cancelar convite?"
                    descricao={`O link enviado para ${convite.email} para de funcionar na hora.`}
                    onConfirmar={() => cancelar(convite)}
                    disabled={cancelarMutation.isPending}
                  />
                )}
              </div>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
