import { useState } from "react";
import { register } from "../api/auth";
import { Link, useNavigate } from "react-router-dom";
import axios from "axios";

export default function RegisterPage() {
  const nav = useNavigate();
  const [fullName, setFullName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [err, setErr] = useState("");

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setErr("");

    try {
      const { token } = await register(email, fullName, password);
      sessionStorage.setItem("token", token);
      nav("/");
    } catch (error) {
      if (axios.isAxiosError(error)) {
        const msg =
          error.response?.data?.errors?.join?.("\n") ??
          error.response?.data?.error ??
          "Register failed";
        setErr(msg);
      } else {
        setErr("Unknown error");
      }
    }
  }

  return (
    <div className="auth">
      <div className="card">
        <h1>Create account</h1>
        <p className="sub">Start tracking your finances</p>

        <form className="form" onSubmit={onSubmit}>
          <div className="field">
            <label>Full name</label>
            <input className="input" value={fullName} onChange={(e) => setFullName(e.target.value)} />
          </div>

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
            Register
          </button>
        </form>

        <div className="helper">
          Already have an account? <Link to="/login">Login</Link>
        </div>
      </div>
    </div>
  );
}