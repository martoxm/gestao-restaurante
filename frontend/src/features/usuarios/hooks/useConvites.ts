import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { cancelarConvite, criarConvite, listarConvites } from "@/features/usuarios/services/convites";

export function useConvites() {
  return useQuery({
    queryKey: ["convites"],
    queryFn: listarConvites,
  });
}

export function useCriarConvite() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: criarConvite,
    onSuccess: (_, { email }) => {
      queryClient.invalidateQueries({ queryKey: ["convites"] });
      toast.success(`Convite enviado para ${email}. O link vale 7 dias.`);
    },
  });
}

export function useCancelarConvite() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: cancelarConvite,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["convites"] });
      toast.success("Convite cancelado. O link parou de funcionar.");
    },
  });
}
