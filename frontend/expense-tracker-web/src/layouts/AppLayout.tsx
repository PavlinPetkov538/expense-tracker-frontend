import { NavLink, Outlet } from "react-router-dom";

export default function AppLayout() {
  return (
    <div className="shell">
      <aside className="sidebar">
        <div className="brand">expense-tracker</div>

        <nav className="nav">
          <NavLink to="/" end className={({ isActive }) => (isActive ? "active" : "")}>
            Dashboard
          </NavLink>
          <NavLink to="/transactions" className={({ isActive }) => (isActive ? "active" : "")}>
            Transactions
          </NavLink>
          <NavLink to="/search" className={({ isActive }) => (isActive ? "active" : "")}>
  Search
</NavLink>

          <NavLink to="/categories" className={({ isActive }) => (isActive ? "active" : "")}>
            Categories
          </NavLink>
          <NavLink to="/settings" className={({ isActive }) => (isActive ? "active" : "")}>
            Settings
          </NavLink>
        </nav>
      </aside>

      <main>
        <div className="topbar">
          <div className="pageTitle">Expense Tracker</div>
          {/* НЯМА Logout тук */}
        </div>

        <div className="content">
          <Outlet />
        </div>
      </main>
    </div>
  );
}
