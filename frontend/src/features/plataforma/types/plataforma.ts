import type { ConviteItemLista } from "@/features/usuarios/types/convite";
import type { ResultadoPaginado } from "@/features/produtos/types/produto";

export type PlanoRestaurante = "Teste" | "Basico" | "Pro";
export type SituacaoRestaurante = "Inativo" | "Pago" | "PlanoVencido" | "EmTeste" | "VencendoEmBreve" | "TesteVencido";

export interface ResumoPlataforma {
  restaurantes: {
    total: number; ativos: number; inativos: number;
    planoTeste: number; planoBasico: number; planoPro: number;
    emTeste: number; testeVencido: number; vencendoEmBreve: number;
  };
  usuarios: { total: number; admins: number; funcionarios: number; desativados: number };
  convites: { pendentes: number; aceitos: number };
  funil: { cadastrados: number; emTeste: number; pagantes: number; vencidosSemConverter: number };
  mrrEstimado: number;
  cadastrosPorDia: { dia: string; quantidade: number }[];
}

export interface RestaurantePlataformaItem {
  id: string;
  nome: string;
  plano: PlanoRestaurante;
  ativo: boolean;
  situacao: SituacaoRestaurante;
  testeTerminaEm: string | null;
  criadoEm: string;
  emailDono: string | null;
  quantidadeUsuarios: number;
  planoValidoAte: string | null;
  assinadoEm: string | null;
  ultimoPagamentoEm: string | null;
}

export interface FiltroRestaurantesPlataforma {
  busca?: string;
  plano?: PlanoRestaurante;
  status?: SituacaoRestaurante | "Ativo";
  pagina: number;
  tamanhoPagina: number;
}

export interface UsuarioPlataforma {
  id: string; nome: string; email: string; role: string; ativo: boolean; emailConfirmado: boolean;
}

export interface RestaurantePlataformaDetalhes extends Omit<RestaurantePlataformaItem, "emailDono" | "quantidadeUsuarios"> {
  limiteUsuarios: number | null;
  usuarios: UsuarioPlataforma[];
  convites: ConviteItemLista[];
}

export interface AuditoriaItem {
  id: string;
  ocorridoEm: string;
  emailSuperAdmin: string;
  acao: string;
  restauranteId: string | null;
  nomeRestaurante: string | null;
  usuarioAlvoId: string | null;
  valorAntes: string | null;
  valorDepois: string | null;
}

export type PaginaRestaurantes = ResultadoPaginado<RestaurantePlataformaItem>;
export type PaginaAuditoria = ResultadoPaginado<AuditoriaItem>;

export const ROTULO_SITUACAO: Record<SituacaoRestaurante, string> = {
  Inativo: "Inativo",
  Pago: "Pago",
  EmTeste: "Em teste",
  VencendoEmBreve: "Vencendo em breve",
  TesteVencido: "Teste vencido",
  PlanoVencido: "Plano vencido",
};

export const ROTULO_PLANO: Record<PlanoRestaurante, string> = { Teste: "Teste", Basico: "Básico", Pro: "Pro" };
