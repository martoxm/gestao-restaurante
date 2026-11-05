"use client";

import { useEffect, useState } from "react";
import { obterToken } from "@/lib/sessao";
import { decodeToken } from "@/lib/jwt";
import { ROLE_CLAIM, type DecodedToken } from "@/features/auth/types/auth";

const ADMIN_ROLE = "Admin";

export function useUsuarioAtual() {
  const [roles, setRoles] = useState<string[]>([]);
  const [restauranteId, setRestauranteId] = useState<string | undefined>();
  const [usuarioId, setUsuarioId] = useState<string | undefined>();
  const [restauranteLiberado, setRestauranteLiberado] = useState(true);
  const [emailConfirmado, setEmailConfirmado] = useState(true);

  useEffect(() => {
    const token = obterToken();
    const decoded = token ? decodeToken<DecodedToken>(token) : null;
    const rawRoles = decoded?.[ROLE_CLAIM] as string | string[] | undefined;

    setRoles(!rawRoles ? [] : Array.isArray(rawRoles) ? rawRoles : [rawRoles]);
    setRestauranteId(decoded?.restauranteId);
    setUsuarioId(decoded?.sub);
    setRestauranteLiberado(decoded?.restauranteLiberado !== "false");
    setEmailConfirmado(decoded?.emailConfirmado !== "false");
  }, []);

  return {
    roles,
    isAdmin: roles.includes(ADMIN_ROLE),
    isSuperAdmin: roles.includes("SuperAdmin"),
    restauranteId,
    usuarioId,
    restauranteLiberado,
    emailConfirmado,
  };
}
