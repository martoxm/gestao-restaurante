import axios, { type AxiosError, type AxiosInstance, type InternalAxiosRequestConfig } from "axios";
import type { LoginResponse } from "@/features/auth/types/auth";
import { limparSessao, obterRefreshToken, obterToken, salvarSessao } from "@/lib/sessao";

const AUTH_PUBLIC_PATHS = ["/auth/login", "/auth/registrar", "/auth/renovar"];

const URL_GATEWAY = process.env.NEXT_PUBLIC_API_URL;

interface ConfigComRetentativa extends InternalAxiosRequestConfig {
  _jaRenovou?: boolean;
}

let renovacaoEmAndamento: Promise<string> | null = null;

function renovarSessao(): Promise<string> {
  if (!renovacaoEmAndamento) {
    renovacaoEmAndamento = (async () => {
      const refreshToken = obterRefreshToken();
      if (!refreshToken) throw new Error("Sem refresh token.");

      const { data } = await axios.post<LoginResponse>(`${URL_GATEWAY}/identidade/api/v1/auth/renovar`, { refreshToken });
      salvarSessao(data);
      return data.token;
    })().finally(() => {
      renovacaoEmAndamento = null;
    });
  }

  return renovacaoEmAndamento;
}

function irParaLogin() {
  limparSessao();
  window.location.href = "/login";
}

function criarClienteApi(prefixoNoGateway: string): AxiosInstance {
  const cliente = axios.create({
    baseURL: `${URL_GATEWAY}/${prefixoNoGateway}/api/v1`,
    headers: {
      "Content-Type": "application/json",
    },
  });

  cliente.interceptors.request.use((config) => {
    const token = obterToken();

    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }

    return config;
  });

  cliente.interceptors.response.use(
    (response) => response,
    async (error: AxiosError) => {
      const config = error.config as ConfigComRetentativa | undefined;
      const requestUrl = config?.url ?? "";
      const isPublicAuthRequest = AUTH_PUBLIC_PATHS.some((path) =>
        requestUrl.includes(path)
      );

      if (
        error.response?.status !== 401 ||
        isPublicAuthRequest ||
        !config ||
        typeof window === "undefined"
      ) {
        return Promise.reject(error);
      }

      if (config._jaRenovou) {
        irParaLogin();
        return Promise.reject(error);
      }

      try {
        const novoToken = await renovarSessao();
        config._jaRenovou = true;
        config.headers.Authorization = `Bearer ${novoToken}`;
        return cliente(config);
      } catch {
        irParaLogin();
        return Promise.reject(error);
      }
    }
  );

  return cliente;
}


export const api = criarClienteApi("estoque");

export const apiIdentidade = criarClienteApi("identidade");

export const apiVendas = criarClienteApi("vendas");
