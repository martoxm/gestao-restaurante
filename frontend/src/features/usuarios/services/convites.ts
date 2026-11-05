import { apiIdentidade } from "@/lib/axios";
import type {
  ConviteCriadoResponse,
  ConviteItemLista,
  CriarConviteRequest,
} from "@/features/usuarios/types/convite";

export async function listarConvites(): Promise<ConviteItemLista[]> {
  const response = await apiIdentidade.get<ConviteItemLista[]>("/convites");

  return response.data;
}

export async function criarConvite(data: CriarConviteRequest): Promise<ConviteCriadoResponse> {
  const response = await apiIdentidade.post<ConviteCriadoResponse>("/convites", data);

  return response.data;
}

export async function cancelarConvite(id: string): Promise<void> {
  await apiIdentidade.delete(`/convites/${id}`);
}
