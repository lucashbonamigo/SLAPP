import { useState, type FormEvent } from "react";
import { login, setToken, ApiError } from "../api";
import SystemLogo from "./SystemLogo";

interface Props {
  onLogin: () => void;
  onRegister: () => void;
}

export default function Login({ onLogin, onRegister }: Props) {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      const result = await login(email, password);
      setToken(result.accessToken);
      onLogin();
    } catch (err) {
      const message =
        err instanceof ApiError ? err.message : "Falha ao conectar ao Sistema.";
      setError(message);
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="auth-screen">
      <form className="panel auth-card" onSubmit={handleSubmit}>
        <SystemLogo />
        <p className="system-subtitle">Autenticação do Jogador</p>

        <label className="field">
          <span>E-mail</span>
          <input
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder="seuemail@dominio.com"
            required
            autoFocus
          />
        </label>

        <label className="field">
          <span>Senha</span>
          <input
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            placeholder="••••••••"
            required
          />
        </label>

        {error && <div className="alert alert-error">{error}</div>}

        <button className="btn btn-primary" type="submit" disabled={loading}>
          {loading ? "Verificando..." : "Entrar no Sistema"}
        </button>

        <button type="button" className="link-btn" onClick={onRegister}>
          Criar novo jogador
        </button>
      </form>
    </div>
  );
}
