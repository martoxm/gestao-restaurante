import { apiPagamentos } from "@/lib/axios";
import type { CheckoutAssinaturaResponse, PlanoAVenda, PlanoPago } from "@/features/assinatura/types/assinatura";

export async function listarPlanos(): Promise<PlanoAVenda[]> {
  const response = await apiPagamentos.get<PlanoAVenda[]>("/assinaturas/planos");
  return response.data;
}

export async function iniciarAssinatura(plano: PlanoPago): Promise<CheckoutAssinaturaResponse> {
  const response = await apiPagamentos.post<CheckoutAssinaturaResponse>("/assinaturas/checkout", { plano });
  return response.data;
}
