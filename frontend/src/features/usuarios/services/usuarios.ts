import { apiIdentidade } from "@/lib/axios";
import type { RoleUsuario, UsuarioItemLista } from "@/features/usuarios/types/usuario";

export async function listarUsuarios(): Promise<UsuarioItemLista[]> {
  const response = await apiIdentidade.get<UsuarioItemLista[]>("/usuarios");

  return response.data;
}

export async function alterarRoleUsuario(id: string, role: RoleUsuario): Promise<void> {
  await apiIdentidade.put(`/usuarios/${id}/role`, { role });
}

export async function alterarAtivoUsuario(id: string, ativo: boolean): Promise<void> {
  await apiIdentidade.put(`/usuarios/${id}/ativo`, { ativo });
}
