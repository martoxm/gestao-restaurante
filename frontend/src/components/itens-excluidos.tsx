"use client";

import { useState } from "react";
import { toast } from "sonner";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api } from "@/lib/axios";
import { obterMensagemErro } from "@/lib/api-errors";
import { formatarData } from "@/lib/datas";
import { Button } from "@/components/ui/button";

type ItemExcluido = { id: string; nome: string; excluidoEm: string };

type ItensExcluidosProps = {
  rota: string; // ex.: "/produtos"
  chaveConsulta: string; // a queryKey da listagem, pra ela se atualizar depois de reativar
  rotulo: string; // ex.: "produtos"
};

export function ItensExcluidos({ rota, chaveConsulta, rotulo }: ItensExcluidosProps) {
  const [aberto, setAberto] = useState(false);
  const queryClient = useQueryClient();

  const { data, isLoading } = useQuery({
    queryKey: [chaveConsulta, "excluidos"],
    queryFn: async () => (await api.get<ItemExcluido[]>(`${rota}/excluidos`)).data,
    enabled: aberto,
  });

  const reativar = useMutation({
    mutationFn: async (id: string) => {
      await api.put(`${rota}/${id}/reativar`);
    },
    onSuccess: () => {
      toast.success("Reativado");
      queryClient.invalidateQueries({ queryKey: [chaveConsulta] });
    },
    onError: (error) => toast.error(obterMensagemErro(error, "Não foi possível reativar.")),
  });

  return (
    <div className="mt-8 rounded-lg border p-4">
      <button
        type="button"
        className="text-sm font-medium text-muted-foreground hover:text-foreground"
        onClick={() => setAberto((valor) => !valor)}
      >
        {aberto ? "▾" : "▸"} {rotulo[0].toUpperCase() + rotulo.slice(1)} excluídos
      </button>

      {aberto && (
        <div className="mt-3 space-y-2">
          {isLoading && <p className="text-sm text-muted-foreground">Carregando...</p>}
          {data?.length === 0 && (
            <p className="text-sm text-muted-foreground">Nenhum registro excluído.</p>
          )}
          {data?.map((item) => (
            <div key={item.id} className="flex items-center justify-between gap-2 text-sm">
              <span>
                {item.nome}{" "}
                <span className="text-muted-foreground">
                  (excluído em {formatarData(item.excluidoEm)})
                </span>
              </span>
              <Button
                variant="outline"
                size="sm"
                disabled={reativar.isPending}
                onClick={() => reativar.mutate(item.id)}
              >
                Reativar
              </Button>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
