import { useState, type FormEvent } from "react";
import { register, setToken, ApiError } from "../api";
import SystemLogo from "./SystemLogo";

interface Props {
  onRegistered: () => void;
  onBackToLogin: () => void;
}

export default function Register({ onRegistered, onBackToLogin }: Props) {
  const [username, setUsername] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirm, setConfirm] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);

    if (password.length < 6) {
      setError("A senha deve ter ao menos 6 caracteres.");
      return;
    }
    if (password !== confirm) {
      setError("As senhas não coincidem.");
      return;
    }

    setLoading(true);
    try {
      const result = await register(username, email, password);
      setToken(result.accessToken);
      onRegistered();
    } catch (err) {
      const message =
        err instanceof ApiError
          ? err.status === 409
            ? "Já existe um jogador com este e-mail."
            : err.message
          : "Falha ao conectar ao Sistema.";
      setError(message);
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="auth-screen">
      <form className="panel auth-card" onSubmit={handleSubmit}>
        <SystemLogo />
        <p className="system-subtitle">Registro de Novo Jogador</p>

        <label className="field">
          <span>Nome do jogador</span>
          <input
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            placeholder="Jogador_01"
            maxLength={50}
            required
            autoFocus
          />
        </label>

        <label className="field">
          <span>E-mail</span>
          <input
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder="seuemail@dominio.com"
            required
          />
        </label>

        <label className="field">
          <span>Senha</span>
          <input
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            placeholder="mínimo 6 caracteres"
            required
          />
        </label>

        <label className="field">
          <span>Confirmar senha</span>
          <input
            type="password"
            value={confirm}
            onChange={(e) => setConfirm(e.target.value)}
            placeholder="••••••••"
            required
          />
        </label>

        {error && <div className="alert alert-error">{error}</div>}

        <button className="btn btn-primary" type="submit" disabled={loading}>
          {loading ? "Registrando..." : "Entrar no Sistema"}
        </button>

        <button type="button" className="link-btn" onClick={onBackToLogin}>
          Já tenho conta — voltar ao login
        </button>
      </form>
    </div>
  );
}
