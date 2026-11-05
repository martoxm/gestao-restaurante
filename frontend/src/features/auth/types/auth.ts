export interface LoginRequest {
  email: string;
  senha: string;
}

export interface LoginResponse {
  token: string;
  refreshToken: string;
  nome: string;
  email: string;
}

export interface RegistrarRequest {
  nome: string;
  email: string;
  senha: string;
  nomeRestaurante: string;
}


export const ROLE_CLAIM =
  "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

export interface DecodedToken {
  sub: string;
  email: string;
  restauranteId?: string;
  restauranteLiberado?: string;
  emailConfirmado?: string;
  [claim: string]: unknown;
}

export interface ConvitePublico {
  email: string;
  nomeRestaurante: string;
  role: "Admin" | "Funcionario";
  expiraEm: string;
}

export interface AceitarConviteRequest {
  token: string;
  nome: string;
  senha: string;
}

export interface RedefinirSenhaRequest {
  email: string;
  token: string;
  novaSenha: string;
}
