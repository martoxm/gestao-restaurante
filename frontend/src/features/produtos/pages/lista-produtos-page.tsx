"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import type { ProdutoItemLista } from "@/features/produtos/types/produto";
import { ArrowDown, ArrowUp, ArrowUpDown, Package } from "lucide-react";
import { toast } from "sonner";
import { obterMensagemErro } from "@/lib/api-errors";
import { formatarData } from "@/lib/datas";
import { useProdutos } from "@/features/produtos/hooks/useProdutos";
import { useCategorias } from "@/features/categorias/hooks/useCategorias";
import { useUsuarioAtual } from "@/features/auth/hooks/useUsuarioAtual";
import { useExcluirProduto } from "@/features/produtos/hooks/useExcluirProduto";
import type {
  DirecaoOrdenacao,
  OrdenacaoProduto,
} from "@/features/produtos/services/produtos";
import { MovimentarProdutoDialog } from "@/features/movimentacoes-estoque/components/movimentar-produto-dialog";
import { ConfirmarExclusao } from "@/components/confirmar-exclusao";
import { TelaVazia } from "@/components/tela-vazia";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";

const formatadorMoeda = new Intl.NumberFormat("pt-BR", {
  style: "currency",
  currency: "BRL",
});

const LIMITE_ESTOQUE_BAIXO = 10;
const TAMANHO_PAGINA = 10;
const TODAS_CATEGORIAS = "todas";

function CabecalhoOrdenavel({
  coluna,
  rotulo,
  ordenarPor,
  direcao,
  onOrdenar,
}: {
  coluna: OrdenacaoProduto;
  rotulo: string;
  ordenarPor: OrdenacaoProduto;
  direcao: DirecaoOrdenacao;
  onOrdenar: (coluna: OrdenacaoProduto) => void;
}) {
  const ativa = coluna === ordenarPor;
  const Icone = !ativa ? ArrowUpDown : direcao === "Asc" ? ArrowUp : ArrowDown;
  return (
    <TableHead aria-sort={ativa ? (direcao === "Asc" ? "ascending" : "descending") : "none"}>
      <button
        type="button"
        onClick={() => onOrdenar(coluna)}
        className="inline-flex items-center gap-1 hover:text-foreground"
      >
        {rotulo}
        <Icone className={`size-3.5 ${ativa ? "" : "opacity-40"}`} />
      </button>
    </TableHead>
  );
}

export function ListaProdutosPage() {
  const [busca, setBusca] = useState("");
  const [buscaAplicada, setBuscaAplicada] = useState("");
  const [categoriaId, setCategoriaId] = useState(TODAS_CATEGORIAS);
  const [ordenarPor, setOrdenarPor] = useState<OrdenacaoProduto>("Nome");
  const [direcao, setDirecao] = useState<DirecaoOrdenacao>("Asc");
  const [pagina, setPagina] = useState(1);

  useEffect(() => {
    const timer = setTimeout(() => {
      setBuscaAplicada(busca.trim());
      setPagina(1);
    }, 400);
    return () => clearTimeout(timer);
  }, [busca]);

  const { data, isLoading, isError, isFetching } = useProdutos({
    pagina,
    tamanhoPagina: TAMANHO_PAGINA,
    nome: buscaAplicada || undefined,
    categoriaId: categoriaId === TODAS_CATEGORIAS ? undefined : categoriaId,
    ordenarPor,
    direcaoOrdenacao: direcao,
  });
  const { data: categorias } = useCategorias();
  const { isAdmin } = useUsuarioAtual();
  const excluirProdutoMutation = useExcluirProduto();

  function handleExcluir(id: string) {
    excluirProdutoMutation.mutate(id, {
      onError: (error) =>
        toast.error(obterMensagemErro(error, "Não foi possível excluir o produto.")),
    });
  }

  function ordenarPorColuna(coluna: OrdenacaoProduto) {
    if (coluna === ordenarPor) {
      setDirecao(direcao === "Asc" ? "Desc" : "Asc");
    } else {
      setOrdenarPor(coluna);
      setDirecao("Asc");
    }
    setPagina(1);
  }

  const ordem = { ordenarPor, direcao, onOrdenar: ordenarPorColuna };
  function renderizarAcoes(produto: ProdutoItemLista) {
    return (
      <div className="flex flex-wrap justify-end gap-2">
        <MovimentarProdutoDialog
          produtoId={produto.id}
          nomeProduto={produto.nome}
          quantidadeEmEstoque={produto.quantidadeEmEstoque}
        />
        <Button variant="outline" size="sm" asChild>
          <Link href={`/produtos/${produto.id}/editar`}>Editar</Link>
        </Button>
        {isAdmin && (
          <ConfirmarExclusao
            titulo={`Excluir o produto "${produto.nome}"?`}
            descricao="Produto que já teve movimentação de estoque não pode ser excluído."
            disabled={excluirProdutoMutation.isPending}
            onConfirmar={() => handleExcluir(produto.id)}
          />
        )}
      </div>
    );
  }

  const temFiltro = buscaAplicada !== "" || categoriaId !== TODAS_CATEGORIAS;

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h1 className="font-heading text-lg font-semibold">Produtos</h1>
        <div className="flex gap-2">
          <Button variant="outline" asChild>
            <Link href="/movimentacoes/nova">Nova movimentação</Link>
          </Button>
          <Button asChild>
            <Link href="/produtos/novo">Novo produto</Link>
          </Button>
        </div>
      </div>

      <div className="flex flex-col gap-2 sm:flex-row">
        <Input
          placeholder="Buscar por nome..."
          value={busca}
          onChange={(e) => setBusca(e.target.value)}
          className="sm:max-w-xs"
          aria-label="Buscar produto por nome"
        />
        <Select
          value={categoriaId}
          onValueChange={(valor) => {
            setCategoriaId(valor);
            setPagina(1);
          }}
        >
          <SelectTrigger className="sm:w-56" aria-label="Filtrar por categoria">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value={TODAS_CATEGORIAS}>Todas as categorias</SelectItem>
            {categorias?.itens.map((categoria) => (
              <SelectItem key={categoria.id} value={categoria.id}>
                {categoria.nome}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      {isLoading && (
        <div className="flex flex-col gap-2">
          {Array.from({ length: 5 }).map((_, i) => (
            <Skeleton key={i} className="h-10 w-full" />
          ))}
        </div>
      )}

      {isError && (
        <p className="text-sm text-destructive">
          Não foi possível carregar os produtos. Verifique se a API está rodando.
        </p>
      )}

      {data && data.itens.length === 0 && (
        temFiltro ? (
          <TelaVazia
            icone={Package}
            titulo="Nenhum produto encontrado"
            descricao="Tente outro nome ou outra categoria."
          />
        ) : (
          <TelaVazia
            icone={Package}
            titulo="Nenhum produto ainda"
            descricao="Cadastre o primeiro produto pra controlar o estoque dele."
            acao={{ href: "/produtos/novo", rotulo: "Novo produto" }}
          />
        )
      )}

      {data && data.itens.length > 0 && (
        <>
          <div className="hidden sm:block">
            <Table className={isFetching ? "opacity-60 transition-opacity" : undefined}>
              <TableHeader>
                <TableRow>
                  <CabecalhoOrdenavel coluna="Nome" rotulo="Nome" {...ordem} />
                  <TableHead>Categoria</TableHead>
                  <CabecalhoOrdenavel coluna="Preco" rotulo="Preço" {...ordem} />
                  <CabecalhoOrdenavel coluna="QuantidadeEmEstoque" rotulo="Estoque" {...ordem} />
                  <TableHead>Criado em</TableHead>
                  <TableHead className="text-right">Ações</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {data.itens.map((produto) => (
                  <TableRow key={produto.id}>
                    <TableCell>{produto.nome}</TableCell>
                    <TableCell>{produto.nomeCategoria}</TableCell>
                    <TableCell>{formatadorMoeda.format(produto.preco)}</TableCell>
                    <TableCell>
                      <div className="flex items-center gap-2">
                        {produto.quantidadeEmEstoque}
                        {produto.quantidadeEmEstoque < LIMITE_ESTOQUE_BAIXO && (
                          <Badge variant="destructive">Estoque baixo</Badge>
                        )}
          </div>
                  </TableCell>
                  <TableCell>{formatarData(produto.criadoEm)}</TableCell>
                  <TableCell>{renderizarAcoes(produto)}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
          </div>
          <div className="flex flex-col gap-3 sm:hidden">
            {data.itens.map((produto) => (
              <div key={produto.id} className="flex flex-col gap-1 rounded-lg border p-4">
                <div className="flex items-start justify-between gap-2">
                  <p className="font-medium">{produto.nome}</p>
                  <span className="text-sm font-medium">
                    {formatadorMoeda.format(produto.preco)}
                  </span>
                </div>
                <p className="text-sm text-muted-foreground">
                  {produto.nomeCategoria} · criado em {formatarData(produto.criadoEm)}
                </p>
                <div className="flex items-center gap-2 text-sm">
                  Estoque: {produto.quantidadeEmEstoque}
                  {produto.quantidadeEmEstoque < LIMITE_ESTOQUE_BAIXO && (
                    <Badge variant="destructive">Estoque baixo</Badge>
                  )}
                </div>
                <div className="mt-2">{renderizarAcoes(produto)}</div>
              </div>
            ))}
          </div>

          {data.totalPaginas > 1 && (
            <div className="flex items-center justify-end gap-2 text-sm">
              <Button
                variant="outline"
                size="sm"
                disabled={pagina <= 1}
                onClick={() => setPagina(pagina - 1)}
              >
                Anterior
              </Button>
              <span className="text-muted-foreground">
                Página {data.pagina} de {data.totalPaginas} · {data.totalItens} produtos
              </span>
              <Button
                variant="outline"
                size="sm"
                disabled={pagina >= data.totalPaginas}
                onClick={() => setPagina(pagina + 1)}
              >
                Próxima
              </Button>
            </div>
          )}
        </>
      )}
    </div>
  );
}
