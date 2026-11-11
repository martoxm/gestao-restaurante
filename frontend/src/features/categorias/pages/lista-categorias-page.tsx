"use client";

import { toast } from "sonner";
import { useAvisoSemPermissao } from "@/lib/use-aviso-sem-permissao";
import Link from "next/link";
import type { CategoriaItemLista } from "@/features/categorias/types/categoria";
import { obterMensagemErro } from "@/lib/api-errors";
import { formatarData } from "@/lib/datas";
import { useCategorias } from "@/features/categorias/hooks/useCategorias";
import { useUsuarioAtual } from "@/features/auth/hooks/useUsuarioAtual";
import { useExcluirCategoria } from "@/features/categorias/hooks/useExcluirCategoria";
import { Tags } from "lucide-react";
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

export function ListaCategoriasPage() {
  useAvisoSemPermissao("/categorias", "Você não tem permissão para editar categorias — essa ação é restrita a administradores.");
  const { data, isLoading, isError } = useCategorias();
  const { isAdmin } = useUsuarioAtual();
  const excluirCategoriaMutation = useExcluirCategoria();


  function handleExcluir(id: string) {
    excluirCategoriaMutation.mutate(id, {
      onError: (error) =>
        toast.error(obterMensagemErro(error, "Não foi possível excluir a categoria.")),
    });
  }

  function renderizarAcoes(categoria: CategoriaItemLista) {
    return isAdmin ? (
        <div className="flex justify-end gap-2">
          <Button variant="outline" size="sm" asChild>
            <Link href={`/categorias/${categoria.id}/editar`}>
              Editar
            </Link>
          </Button>
          <ConfirmarExclusao
            titulo={`Excluir a categoria "${categoria.nome}"?`}
            descricao="Se ainda houver produtos nela, o sistema vai recusar."
            disabled={excluirCategoriaMutation.isPending}
            onConfirmar={() => handleExcluir(categoria.id)}
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
        <h1 className="font-heading text-lg font-semibold">Categorias</h1>
        <Button asChild>
          <Link href="/categorias/nova">Nova categoria</Link>
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
          Não foi possível carregar as categorias. Verifique se a API está
          rodando e se o CORS já foi configurado (etapa 12 do backend).
        </p>
      )}

      {data && data.itens.length === 0 && (
        <TelaVazia
          icone={Tags}
          titulo="Nenhuma categoria ainda"
          descricao="Categorias agrupam os produtos (bebidas, carnes, limpeza...). Crie a primeira pra começar a cadastrar produtos."
          acao={{ href: "/categorias/nova", rotulo: "Nova categoria" }}
        />
      )}

      {data && data.itens.length > 0 && (
        <>
          <div className="hidden sm:block">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Nome</TableHead>
                  <TableHead>Descrição</TableHead>
                  <TableHead>Produtos vinculados</TableHead>
                  <TableHead>Criado em</TableHead>
                  <TableHead className="text-right">Ações</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {data.itens.map((categoria) => (
                  <TableRow key={categoria.id}>
                    <TableCell>{categoria.nome}</TableCell>
                    <TableCell>{categoria.descricao || "—"}</TableCell>
                    <TableCell>{categoria.quantidadeProdutos}</TableCell>
                    <TableCell>{formatarData(categoria.criadoEm)}</TableCell>
                    <TableCell>{renderizarAcoes(categoria)}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
          <div className="flex flex-col gap-3 sm:hidden">
            {data.itens.map((categoria) => (
              <div key={categoria.id} className="flex flex-col gap-1 rounded-lg border p-4">
                <p className="font-medium">{categoria.nome}</p>
                <p className="text-sm text-muted-foreground">{categoria.descricao || "Sem descrição"}</p>
                <p className="text-sm text-muted-foreground">Criado em {formatarData(categoria.criadoEm)}</p>
                <p className="text-sm">{categoria.quantidadeProdutos} produto(s) vinculado(s)</p>
                <div className="mt-2">{renderizarAcoes(categoria)}</div>
              </div>
            ))}
          </div>
        </>
      )}
      {isAdmin && <ItensExcluidos rota="/categorias" chaveConsulta="categorias" rotulo="categorias" />}
    </div>
  );
}
