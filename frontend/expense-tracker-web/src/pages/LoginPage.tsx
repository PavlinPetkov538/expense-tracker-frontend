import { useState } from "react";
import { login } from "../api/auth";
import { Link, useNavigate } from "react-router-dom";
import axios from "axios";

export default function LoginPage() {
  const nav = useNavigate();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [err, setErr] = useState("");

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setErr("");

    try {
      const { token } = await login(email, password);
      sessionStorage.setItem("token", token);
      nav("/");
    } catch (error) {
      if (axios.isAxiosError(error)) {
        setErr(error.response?.data?.error ?? "Invalid credentials");
      } else {
        setErr("Unknown error");
      }
    }
  }

  return (
    <div className="auth">
      <div className="card">
        <h1>Welcome back</h1>
        <p className="sub">Sign in to continue</p>

        <form className="form" onSubmit={onSubmit}>
          <div className="field">
            <label>Email</label>
            <input className="input" value={email} onChange={(e) => setEmail(e.target.value)} />
          </div>

          <div className="field">
            <label>Password</label>
            <input className="input" type="password" value={password} onChange={(e) => setPassword(e.target.value)} />
          </div>

          {err && <div className="error">{err}</div>}

          <button className="btn primary" type="submit">
            Login
          </button>
        </form>

        <div className="helper">
          No account? <Link to="/register">Create one</Link>
        </div>
      </div>
    </div>
  );
}
