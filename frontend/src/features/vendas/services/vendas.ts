import { apiVendas } from "@/lib/axios";
import type { ResultadoPaginado } from "@/features/produtos/types/produto";
import type {
  CriarVendaRequest,
  VendaDetalhes,
  VendaItemLista,
} from "@/features/vendas/types/venda";

export async function listarVendas(): Promise<ResultadoPaginado<VendaItemLista>> {
  const response = await apiVendas.get<ResultadoPaginado<VendaItemLista>>("/vendas", {
    params: { tamanhoPagina: 100 },
  });

  return response.data;
}

export async function obterVendaPorId(id: string): Promise<VendaDetalhes> {
  const response = await apiVendas.get<VendaDetalhes>(`/vendas/${id}`);

  return response.data;
}

export async function criarVenda(data: CriarVendaRequest): Promise<{ id: string }> {
  const response = await apiVendas.post<{ id: string }>("/vendas", data);

  return response.data;
}
