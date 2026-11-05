import { apiIdentidade } from "@/lib/axios";
import type { AceitarConviteRequest, RedefinirSenhaRequest, ConvitePublico, LoginRequest, LoginResponse, RegistrarRequest } from "@/features/auth/types/auth";

export async function login(data: LoginRequest): Promise<LoginResponse> {
  const response = await apiIdentidade.post<LoginResponse>("/auth/login", data);

  return response.data;
}

export async function registrar(data: RegistrarRequest): Promise<LoginResponse> {
  const response = await apiIdentidade.post<LoginResponse>("/auth/registrar", data);

  return response.data;
}

export async function obterConvite(token: string): Promise<ConvitePublico> {
  const response = await apiIdentidade.get<ConvitePublico>("/auth/convite", { params: { token } });

  return response.data;
}

export async function aceitarConvite(data: AceitarConviteRequest): Promise<LoginResponse> {
  const response = await apiIdentidade.post<LoginResponse>("/auth/aceitar-convite", data);

  return response.data;
}


export async function esqueciSenha(email: string): Promise<void> {
  await apiIdentidade.post("/auth/esqueci-senha", { email });
}

export async function redefinirSenha(data: RedefinirSenhaRequest): Promise<void> {
  await apiIdentidade.post("/auth/redefinir-senha", data);
}

export async function confirmarEmail(usuarioId: string, token: string): Promise<void> {
  await apiIdentidade.post("/auth/confirmar-email", { usuarioId, token });
}

export async function reenviarConfirmacaoEmail(): Promise<void> {
  await apiIdentidade.post("/auth/reenviar-confirmacao");
}
