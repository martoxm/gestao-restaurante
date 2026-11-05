import { useMutation, useQuery } from "@tanstack/react-query";
import { useRouter } from "next/navigation";
import { salvarSessao } from "@/lib/sessao";
import { aceitarConvite, obterConvite } from "@/features/auth/services/auth";

export function useConvitePublico(token: string | null) {
  return useQuery({
    queryKey: ["convite-publico", token],
    queryFn: () => obterConvite(token!),
    enabled: !!token,
    retry: false,
  });
}

export function useAceitarConvite() {
  const router = useRouter();

  return useMutation({
    mutationFn: aceitarConvite,
    onSuccess: (data) => {
      salvarSessao(data);
      router.push("/");
    },
  });
}
