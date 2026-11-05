import type { RoleUsuario } from "@/features/usuarios/types/usuario";

export type StatusConvite = "Pendente" | "Aceito" | "Cancelado" | "Expirado";

export interface ConviteItemLista {
  id: string;
  email: string;
  role: RoleUsuario;
  status: StatusConvite;
  criadoEm: string;
  expiraEm: string;
}

export interface CriarConviteRequest {
  email: string;
  role: RoleUsuario;
}

export interface ConviteCriadoResponse {
  id: string;
  expiraEm: string;
}
