import {
  LayoutDashboard,
  Package,
  ArrowRightLeft,
  Tags,
  Truck,
  ShoppingCart,
  Users,
  type LucideIcon,
} from "lucide-react";

export interface NavItem {
  href: string;
  label: string;
  icon: LucideIcon;
  soAdmin?: boolean;
}

export const NAV_ITEMS: NavItem[] = [
  { href: "/", label: "Dashboard", icon: LayoutDashboard },
  { href: "/produtos", label: "Produtos", icon: Package },
  { href: "/categorias", label: "Categorias", icon: Tags },
  { href: "/fornecedores", label: "Fornecedores", icon: Truck },
  { href: "/vendas", label: "Vendas", icon: ShoppingCart },
  { href: "/movimentacoes", label: "Movimentações", icon: ArrowRightLeft },
  { href: "/usuarios", label: "Usuários", icon: Users, soAdmin: true },
];
