import { useNavigate } from "react-router-dom";

export default function SettingsPage() {
  const nav = useNavigate();

  function logout() {
    sessionStorage.removeItem("token");
    nav("/login");
  }

  return (
    <div className="panel">
      <h2 style={{ margin: "0 0 14px 0" }}>Settings</h2>

      <div className="row">
        <div style={{ color: "var(--muted)", fontSize: 14 }}>Sign out from this session</div>
        <button className="btn outlineDanger" onClick={logout}>
          Logout
        </button>
      </div>
    </div>
  );
}