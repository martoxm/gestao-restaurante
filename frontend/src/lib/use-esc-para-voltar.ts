"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";

export function useEscParaVoltar(destino: string) {
  const router = useRouter();

  useEffect(() => {
    function aoApertarTecla(event: KeyboardEvent) {
      if (event.key !== "Escape" || event.defaultPrevented) return;
      router.push(destino);
    }

    window.addEventListener("keydown", aoApertarTecla);
    return () => window.removeEventListener("keydown", aoApertarTecla);
  }, [router, destino]);
}
