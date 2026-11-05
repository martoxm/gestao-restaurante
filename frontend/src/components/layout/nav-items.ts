import {
  LayoutDashboard,
  Package,
  ArrowRightLeft,
  Tags,
  Truck,
  ShoppingCart,
  Users,
  Building2,
  CreditCard,
  type LucideIcon,
} from "lucide-react";

export interface NavItem {
  href: string;
  label: string;
  icon: LucideIcon;
  soAdmin?: boolean;
  soSuperAdmin?: boolean;
}

export const NAV_ITEMS: NavItem[] = [
  { href: "/", label: "Dashboard", icon: LayoutDashboard },
  { href: "/produtos", label: "Produtos", icon: Package },
  { href: "/categorias", label: "Categorias", icon: Tags },
  { href: "/fornecedores", label: "Fornecedores", icon: Truck },
  { href: "/vendas", label: "Vendas", icon: ShoppingCart },
  { href: "/movimentacoes", label: "Movimentações", icon: ArrowRightLeft },
  { href: "/usuarios", label: "Usuários", icon: Users, soAdmin: true },
  { href: "/assinatura", label: "Assinatura", icon: CreditCard, soAdmin: true },
  { href: "/admin-plataforma", label: "Plataforma", icon: Building2, soSuperAdmin: true },
];

export function itemVisivel(
  item: NavItem,
  { isAdmin, isSuperAdmin }: { isAdmin: boolean; isSuperAdmin: boolean },
): boolean {
  if (isSuperAdmin) return !!item.soSuperAdmin;
  return (!item.soAdmin || isAdmin) && !item.soSuperAdmin;
}
