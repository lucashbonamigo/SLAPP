import type {
  LoginResponse,
  PlayerStatus,
  Quest,
  Punishment,
  PunishmentInput,
  Goal,
  GoalInput,
} from "./types";

const BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "";
const TOKEN_KEY = "sololeveling.token";

export function getToken(): string | null {
  return localStorage.getItem(TOKEN_KEY);
}

export function setToken(token: string): void {
  localStorage.setItem(TOKEN_KEY, token);
}

export function clearToken(): void {
  localStorage.removeItem(TOKEN_KEY);
}

/** Erro de API com o status HTTP, para o UI decidir a mensagem. */
export class ApiError extends Error {
  status: number;
  constructor(status: number, message: string) {
    super(message);
    this.status = status;
  }
}

async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers);
  const token = getToken();
  if (token) {
    headers.set("Authorization", `Bearer ${token}`);
  }

  const response = await fetch(`${BASE_URL}${path}`, { ...init, headers });

  if (response.status === 401) {
    clearToken();
    throw new ApiError(401, "Sessão expirada. Autentique-se novamente.");
  }

  if (!response.ok) {
    const message = await extractError(response);
    throw new ApiError(response.status, message);
  }

  // 204 ou corpo vazio
  const text = await response.text();
  return (text ? JSON.parse(text) : undefined) as T;
}

async function extractError(response: Response): Promise<string> {
  try {
    const data = await response.json();
    return data?.message ?? `Erro ${response.status}`;
  } catch {
    return `Erro ${response.status}`;
  }
}

export async function login(email: string, password: string): Promise<LoginResponse> {
  return request<LoginResponse>("/api/v1/auth/login", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password }),
  });
}

export async function register(
  username: string,
  email: string,
  password: string
): Promise<LoginResponse> {
  return request<LoginResponse>("/api/v1/auth/register", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ username, email, password }),
  });
}

export async function getPlayerStatus(): Promise<PlayerStatus> {
  return request<PlayerStatus>("/api/v1/player/status");
}

export async function getTodayQuests(): Promise<Quest[]> {
  return request<Quest[]>("/api/v1/quests/today");
}

export async function getAllQuests(): Promise<Quest[]> {
  return request<Quest[]>("/api/v1/quests");
}

export async function getGoals(): Promise<Goal[]> {
  return request<Goal[]>("/api/v1/goals");
}

export async function createGoal(input: GoalInput): Promise<Goal> {
  return request<Goal>("/api/v1/goals", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(input),
  });
}

export async function updateGoal(id: number, input: GoalInput): Promise<Goal> {
  return request<Goal>(`/api/v1/goals/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(input),
  });
}

export async function deleteGoal(id: number): Promise<void> {
  await request<void>(`/api/v1/goals/${id}`, { method: "DELETE" });
}

export async function getPunishments(): Promise<Punishment[]> {
  return request<Punishment[]>("/api/v1/punishments");
}

export async function createPunishment(input: PunishmentInput): Promise<Punishment> {
  return request<Punishment>("/api/v1/punishments", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(input),
  });
}

export async function updatePunishment(
  id: number,
  input: PunishmentInput
): Promise<Punishment> {
  return request<Punishment>(`/api/v1/punishments/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(input),
  });
}

export async function deletePunishment(id: number): Promise<void> {
  await request<void>(`/api/v1/punishments/${id}`, { method: "DELETE" });
}

export async function uploadProof(questId: number, file: File): Promise<void> {
  const form = new FormData();
  form.append("file", file);
  await request<void>(`/api/v1/quests/${questId}/proof`, {
    method: "POST",
    body: form,
  });
}
