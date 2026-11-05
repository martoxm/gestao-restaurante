import { NextResponse } from "next/server";
import type { NextRequest } from "next/server";
import { decodeToken } from "@/lib/jwt";
import { ROLE_CLAIM, type DecodedToken } from "@/features/auth/types/auth";

const PUBLIC_PATHS = ["/login", "/registrar", "/esqueci-senha"];

const SEMPRE_LIBERADAS = ["/aceitar-convite", "/redefinir-senha", "/confirmar-email"];

const ROTAS_SO_ADMIN: { padrao: RegExp; voltarPara: string }[] = [
  { padrao: /^\/categorias\/[^/]+\/editar$/, voltarPara: "/categorias" },
  { padrao: /^\/fornecedores\/[^/]+\/editar$/, voltarPara: "/fornecedores" },
  { padrao: /^\/usuarios(\/|$)/, voltarPara: "/" },
  { padrao: /^\/assinatura(\/|$)/, voltarPara: "/" },
];

function temRole(token: string, role: string): boolean {
  const roles = decodeToken<DecodedToken>(token)?.[ROLE_CLAIM];
  return Array.isArray(roles) ? roles.includes(role) : roles === role;
}

export function proxy(request: NextRequest) {
  const { pathname } = request.nextUrl;
  const isPublicPath = PUBLIC_PATHS.includes(pathname);
  const token = request.cookies.get("token")?.value;

  if (SEMPRE_LIBERADAS.includes(pathname)) {
    return NextResponse.next();
  }

  if (!token && !isPublicPath) {
    return NextResponse.redirect(new URL("/login", request.url));
  }

  const ehPlataforma = /^\/admin-plataforma(\/|$)/.test(pathname);

  if (token && !ehPlataforma && temRole(token, "SuperAdmin")) {
    return NextResponse.redirect(new URL("/admin-plataforma", request.url));
  }

  if (token && isPublicPath) {
    return NextResponse.redirect(new URL("/", request.url));
  }

  const rotaSoAdmin = ROTAS_SO_ADMIN.find((rota) => rota.padrao.test(pathname));
  if (token && rotaSoAdmin && !temRole(token, "Admin")) {
    const destino = new URL(rotaSoAdmin.voltarPara, request.url);
    destino.searchParams.set("aviso", "sem-permissao");
    return NextResponse.redirect(destino);
  }

  if (token && ehPlataforma && !temRole(token, "SuperAdmin")) {
    const destino = new URL("/", request.url);
    destino.searchParams.set("aviso", "sem-permissao");
    return NextResponse.redirect(destino);
  }

  return NextResponse.next();
}

export const config = {
  matcher: ["/((?!_next/static|_next/image|favicon.ico).*)"],
};
