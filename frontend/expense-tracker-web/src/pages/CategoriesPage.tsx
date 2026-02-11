import { useEffect, useState } from "react";
import { createCategory, deleteCategory, getCategories } from "../api/categories";
import type { Category } from "../api/categories";
import axios from "axios";

export default function CategoriesPage() {
  const [items, setItems] = useState<Category[]>([]);
  const [name, setName] = useState("");
  const [type, setType] = useState(0);
  const [color, setColor] = useState("#111827");
  const [err, setErr] = useState("");

  async function load() {
    setErr("");
    try {
      setItems(await getCategories());
    } catch {
      setErr("Failed to load categories.");
    }
  }

  useEffect(() => {
    load();
  }, []);

  async function add() {
    setErr("");
    try {
      await createCategory(name, type, color);
      setName("");
      await load();
    } catch (error) {
      if (axios.isAxiosError(error)) {
        const msg =
          error.response?.data?.error ??
          error.response?.data?.errors?.join?.("\n") ??
          "Failed to create category.";
        setErr(msg);
      } else {
        setErr("Failed to create category.");
      }
    }
  }

  async function del(id: string) {
    setErr("");
    try {
      await deleteCategory(id);
      await load();
    } catch (error) {
      if (axios.isAxiosError(error)) {
        const msg = error.response?.data?.error ?? "Failed to delete category.";
        setErr(msg);
      } else {
        setErr("Failed to delete category.");
      }
    }
  }

  return (
    <div className="panel">
      <div className="panelHead">
        <div>
          <div className="panelTitle">Categories</div>
          <div className="muted small">Create and manage your categories</div>
        </div>
      </div>

      <div style={{ display: "grid", gap: 10, marginBottom: 16 }}>
        <div style={{ display: "grid", gridTemplateColumns: "2fr 1fr 1fr auto", gap: 10 }}>
          <input
            className="input"
            placeholder="Name (e.g. Food)"
            value={name}
            onChange={(e) => setName(e.target.value)}
          />
          <select className="input" value={type} onChange={(e) => setType(Number(e.target.value))}>
            <option value={0}>Expense</option>
            <option value={1}>Income</option>
            <option value={2}>Both</option>
          </select>
          <input className="input" type="color" value={color} onChange={(e) => setColor(e.target.value)} />
          <button className="btn primary" onClick={add} disabled={!name.trim()}>
            Add
          </button>
        </div>

        {err && <div className="error">{err}</div>}
      </div>

      <div className="tableWrap">
        <table className="table">
          <thead>
            <tr>
              <th>Name</th>
              <th>Type</th>
              <th>Color</th>
              <th className="right">Actions</th>
            </tr>
          </thead>
          <tbody>
            {items.map((c) => (
              <tr key={c.id}>
                <td>{c.name}</td>
                <td className="muted">{c.type === 0 ? "Expense" : c.type === 1 ? "Income" : "Both"}</td>
                <td className="mono">{c.color ?? "-"}</td>
                <td className="right">
                  <button className="btn outlineDanger" onClick={() => del(c.id)}>
                    Delete
                  </button>
                </td>
              </tr>
            ))}

            {items.length === 0 && (
              <tr>
                <td colSpan={4} className="muted">
                  No categories yet.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
