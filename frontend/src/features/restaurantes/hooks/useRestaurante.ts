import { useQuery } from "@tanstack/react-query";
import { obterRestaurantePorId } from "@/features/restaurantes/services/restaurantes";

export function useRestaurante(id: string | undefined, opcoes?: { refetchInterval?: number | false }) {
  return useQuery({
    queryKey: ["restaurantes", id],
    queryFn: () => obterRestaurantePorId(id as string),
    enabled: !!id,
    refetchInterval: opcoes?.refetchInterval ?? false,
  });
}
