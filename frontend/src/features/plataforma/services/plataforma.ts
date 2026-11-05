import { apiIdentidade } from "@/lib/axios";
import type {
  FiltroRestaurantesPlataforma,
  PaginaAuditoria,
  PaginaRestaurantes,
  PlanoRestaurante,
  RestaurantePlataformaDetalhes,
  ResumoPlataforma,
} from "@/features/plataforma/types/plataforma";

export async function obterResumoPlataforma(): Promise<ResumoPlataforma> {
  return (await apiIdentidade.get<ResumoPlataforma>("/plataforma/resumo")).data;
}

export async function listarRestaurantesPlataforma(filtro: FiltroRestaurantesPlataforma): Promise<PaginaRestaurantes> {
  return (await apiIdentidade.get<PaginaRestaurantes>("/plataforma/restaurantes", { params: filtro })).data;
}

export async function obterRestaurantePlataforma(id: string): Promise<RestaurantePlataformaDetalhes> {
  return (await apiIdentidade.get<RestaurantePlataformaDetalhes>(`/plataforma/restaurantes/${id}`)).data;
}

export async function listarAuditoria(pagina: number, restauranteId?: string): Promise<PaginaAuditoria> {
  return (
    await apiIdentidade.get<PaginaAuditoria>("/plataforma/auditoria", {
      params: { pagina, tamanhoPagina: 10, restauranteId },
    })
  ).data;
}

export async function mudarPlano(id: string, plano: PlanoRestaurante) {
  await apiIdentidade.put(`/plataforma/restaurantes/${id}/plano`, { plano });
}

export async function estenderTeste(id: string, dias: number) {
  await apiIdentidade.post(`/plataforma/restaurantes/${id}/estender-teste`, { dias });
}

export async function alterarAtivoRestaurante(id: string, ativo: boolean) {
  await apiIdentidade.put(`/plataforma/restaurantes/${id}/ativo`, { ativo });
}

export async function reenviarConvite(id: string) {
  await apiIdentidade.post(`/plataforma/convites/${id}/reenviar`);
}

export async function forcarLogout(usuarioId: string) {
  await apiIdentidade.post(`/plataforma/usuarios/${usuarioId}/forcar-logout`);
}

export async function alterarAtivoUsuarioPlataforma(usuarioId: string, ativo: boolean) {
  await apiIdentidade.put(`/plataforma/usuarios/${usuarioId}/ativo`, { ativo });
}
