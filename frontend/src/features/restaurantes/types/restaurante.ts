export interface RestauranteDetalhes {
  id: string;
  nome: string;
  criadoEm: string;
  plano: "Teste" | "Basico" | "Pro";
  testeTerminaEm: string | null;
  ativo: boolean;
  planoValidoAte: string | null;
}
