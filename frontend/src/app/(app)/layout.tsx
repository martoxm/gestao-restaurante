import { Header } from "@/components/layout/header";
import { Sidebar } from "@/components/layout/sidebar";
import { AvisoRestauranteBloqueado } from "@/components/layout/aviso-restaurante-bloqueado";
import { AvisoEmailNaoConfirmado } from "@/components/layout/aviso-email-nao-confirmado";

export default function AppLayout({ children }: LayoutProps<"/">) {
  return (
    <div className="flex flex-1 flex-col">
      <Header />
      <div className="flex flex-1">
        <Sidebar />
        <main className="flex-1 p-6">
          <AvisoRestauranteBloqueado />
          <AvisoEmailNaoConfirmado />
          {children}
        </main>
      </div>
    </div>
  );
}
