"use client";

import { Users } from "lucide-react";
import { toast } from "sonner";
import { obterMensagemErro } from "@/lib/api-errors";
import { useUsuarioAtual } from "@/features/auth/hooks/useUsuarioAtual";
import { useUsuarios } from "@/features/usuarios/hooks/useUsuarios";
import { useAlterarRoleUsuario } from "@/features/usuarios/hooks/useAlterarRoleUsuario";
import type { RoleUsuario, UsuarioItemLista } from "@/features/usuarios/types/usuario";
import { TelaVazia } from "@/components/tela-vazia";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";

export function ListaUsuariosPage() {
  const { data, isLoading, isError } = useUsuarios();
  const { usuarioId } = useUsuarioAtual();
  const alterarRoleMutation = useAlterarRoleUsuario();

  function alterarRole(usuario: UsuarioItemLista, role: RoleUsuario) {
    if (role === usuario.role) return;
    alterarRoleMutation.mutate(
      { id: usuario.id, role },
      {
        onError: (error) =>
          toast.error(obterMensagemErro(error, "Não foi possível trocar a role.")),
      }
    );
  }

  function renderizarSeletor(usuario: UsuarioItemLista) {
    return (
      <Select
        value={usuario.role}
        onValueChange={(valor) => alterarRole(usuario, valor as RoleUsuario)}
        disabled={alterarRoleMutation.isPending}
      >
        <SelectTrigger className="w-40" aria-label={`Role de ${usuario.nome}`}>
          <SelectValue />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="Admin">Admin</SelectItem>
          <SelectItem value="Funcionario">Funcionário</SelectItem>
        </SelectContent>
      </Select>
    );
  }

  function nomeComMarca(usuario: UsuarioItemLista) {
    return (
      <>
        {usuario.nome}
        {usuario.id === usuarioId && (
          <span className="ml-1 text-xs text-muted-foreground">(você)</span>
        )}
      </>
    );
  }

  return (
    <div className="flex flex-col gap-4">
      <div>
        <h1 className="font-heading text-lg font-semibold">Usuários</h1>
        <p className="text-sm text-muted-foreground">
          Quem é Admin pode editar e excluir categorias, fornecedores e produtos, e gerenciar esta tela.
          O restaurante precisa ter sempre pelo menos um Admin.
        </p>
      </div>

      {isLoading && (
        <div className="flex flex-col gap-2">
          {Array.from({ length: 4 }).map((_, i) => (
            <Skeleton key={i} className="h-10 w-full" />
          ))}
        </div>
      )}

      {isError && (
        <p className="text-sm text-destructive">
          Não foi possível carregar os usuários. Verifique se a API de Identidade está rodando.
        </p>
      )}

      {data && data.length === 0 && (
        <TelaVazia icone={Users} titulo="Nenhum usuário" descricao="Ninguém cadastrado neste restaurante." />
      )}

      {data && data.length > 0 && (
        <>
          <div className="hidden sm:block">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Nome</TableHead>
                  <TableHead>E-mail</TableHead>
                  <TableHead>Role</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {data.map((usuario) => (
                  <TableRow key={usuario.id}>
                    <TableCell>{nomeComMarca(usuario)}</TableCell>
                    <TableCell>{usuario.email}</TableCell>
                    <TableCell>{renderizarSeletor(usuario)}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
          <div className="flex flex-col gap-3 sm:hidden">
            {data.map((usuario) => (
              <div key={usuario.id} className="flex flex-col gap-1 rounded-lg border p-4">
                <p className="font-medium">{nomeComMarca(usuario)}</p>
                <p className="text-sm text-muted-foreground">{usuario.email}</p>
                <div className="mt-2">{renderizarSeletor(usuario)}</div>
              </div>
            ))}
          </div>
        </>
      )}
    </div>
  );
}
