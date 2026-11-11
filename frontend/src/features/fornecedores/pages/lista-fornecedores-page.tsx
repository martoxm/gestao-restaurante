"use client";

import { toast } from "sonner";
import { useAvisoSemPermissao } from "@/lib/use-aviso-sem-permissao";
import Link from "next/link";
import type { FornecedorItemLista } from "@/features/fornecedores/types/fornecedor";
import { obterMensagemErro } from "@/lib/api-errors";
import { formatarData } from "@/lib/datas";
import { useUsuarioAtual } from "@/features/auth/hooks/useUsuarioAtual";
import { useFornecedores } from "@/features/fornecedores/hooks/useFornecedores";
import { useExcluirFornecedor } from "@/features/fornecedores/hooks/useExcluirFornecedor";
import { Truck } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { ConfirmarExclusao } from "@/components/confirmar-exclusao";
import { ItensExcluidos } from "@/components/itens-excluidos";
import { TelaVazia } from "@/components/tela-vazia";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";

export function ListaFornecedoresPage() {
  useAvisoSemPermissao("/fornecedores", "Você não tem permissão para editar fornecedores — essa ação é restrita a administradores.");
  const { data, isLoading, isError } = useFornecedores();
  const { isAdmin } = useUsuarioAtual();
  const excluirFornecedorMutation = useExcluirFornecedor();


  function handleExcluir(id: string) {
    excluirFornecedorMutation.mutate(id, {
      onError: (error) =>
        toast.error(obterMensagemErro(error, "Não foi possível excluir o fornecedor.")),
    });
  }

  function renderizarAcoes(fornecedor: FornecedorItemLista) {
    return isAdmin ? (
        <div className="flex justify-end gap-2">
          <Button variant="outline" size="sm" asChild>
            <Link href={`/fornecedores/${fornecedor.id}/editar`}>
              Editar
            </Link>
          </Button>
          <ConfirmarExclusao
            titulo={`Excluir o fornecedor "${fornecedor.nome}"?`}
            descricao="Essa ação não pode ser desfeita."
            disabled={excluirFornecedorMutation.isPending}
            onConfirmar={() => handleExcluir(fornecedor.id)}
          />
        </div>
      ) : (
        <p className="text-right text-sm text-muted-foreground">
          —
        </p>
      );
  }

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center justify-between">
        <h1 className="font-heading text-lg font-semibold">Fornecedores</h1>
        <Button asChild>
          <Link href="/fornecedores/novo">Novo fornecedor</Link>
        </Button>
      </div>

      {isLoading && (
        <div className="flex flex-col gap-2">
          {Array.from({ length: 5 }).map((_, i) => (
            <Skeleton key={i} className="h-10 w-full" />
          ))}
        </div>
      )}

      {isError && (
        <p className="text-sm text-destructive">
          Não foi possível carregar os fornecedores. Verifique se a API está
          rodando.
        </p>
      )}

      {data && data.itens.length === 0 && (
        <TelaVazia
          icone={Truck}
          titulo="Nenhum fornecedor ainda"
          descricao="Guarde aqui o contato de quem abastece o restaurante."
          acao={{ href: "/fornecedores/novo", rotulo: "Novo fornecedor" }}
        />
      )}

      {data && data.itens.length > 0 && (
        <>
          <div className="hidden sm:block">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Nome</TableHead>
                  <TableHead>Telefone</TableHead>
                  <TableHead>E-mail</TableHead>
                  <TableHead>Criado em</TableHead>
                  <TableHead className="text-right">Ações</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {data.itens.map((fornecedor) => (
                  <TableRow key={fornecedor.id}>
                    <TableCell>{fornecedor.nome}</TableCell>
                    <TableCell>{fornecedor.telefone || "—"}</TableCell>
                    <TableCell>{fornecedor.email || "—"}</TableCell>
                    <TableCell>{formatarData(fornecedor.criadoEm)}</TableCell>
                    <TableCell>{renderizarAcoes(fornecedor)}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
          <div className="flex flex-col gap-3 sm:hidden">
            {data.itens.map((fornecedor) => (
              <div key={fornecedor.id} className="flex flex-col gap-1 rounded-lg border p-4">
                <p className="font-medium">{fornecedor.nome}</p>
                <p className="text-sm text-muted-foreground">{fornecedor.telefone || "Sem telefone"}</p>
                <p className="text-sm text-muted-foreground">{fornecedor.email || "Sem e-mail"}</p>
                <p className="text-sm text-muted-foreground">Criado em {formatarData(fornecedor.criadoEm)}</p>
                <div className="mt-2">{renderizarAcoes(fornecedor)}</div>
              </div>
            ))}
          </div>
        </>
      )}
      {isAdmin && <ItensExcluidos rota="/fornecedores" chaveConsulta="fornecedores" rotulo="fornecedores" />}
    </div>
  );
}
