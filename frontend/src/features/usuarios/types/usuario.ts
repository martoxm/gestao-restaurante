export type RoleUsuario = "Admin" | "Funcionario";

export interface UsuarioItemLista {
  id: string;
  nome: string;
  email: string;
  role: RoleUsuario;
}
