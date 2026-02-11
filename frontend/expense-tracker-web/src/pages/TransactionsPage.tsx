import { useEffect, useState } from "react";
import { createTransaction, deleteTransaction, getTransactions } from "../api/transactions";
import type { Transaction } from "../api/transactions";
import { getCategories } from "../api/categories";
import type { Category } from "../api/categories";

export default function TransactionsPage() {
  const [items, setItems] = useState<Transaction[]>([]);
  const [cats, setCats] = useState<Category[]>([]);
  const [err, setErr] = useState("");

  const [amount, setAmount] = useState("");
  const [date, setDate] = useState(() => new Date().toISOString().slice(0, 10));
  const [type, setType] = useState(0);
  const [categoryId, setCategoryId] = useState<string>("");
  const [note, setNote] = useState("");

  async function load() {
    setErr("");
    try {
      const [c, t] = await Promise.all([getCategories(), getTransactions({ take: 50 })]);
      setCats(c);
      setItems(t);
    } catch {
      setErr("Failed to load data.");
    }
  }

  useEffect(() => {
    load();
  }, []);

  async function add() {
    setErr("");
    try {
      const amt = Number(amount);

      await createTransaction({
        amount: amt,
        date,
        type,
        note: note.trim() ? note : undefined,
        categoryId: categoryId ? categoryId : undefined,
      });

      setAmount("");
      setNote("");
      await load();
    } catch {
      setErr("Failed to create transaction.");
    }
  }

  async function del(id: string) {
    setErr("");
    try {
      await deleteTransaction(id);
      await load();
    } catch {
      setErr("Failed to delete transaction.");
    }
  }

  return (
    <div className="panel">
      <div className="panelHead">
        <div>
          <div className="panelTitle">Transactions</div>
          <div className="muted small">Add and review your transactions</div>
        </div>
      </div>

      <div style={{ display: "grid", gap: 10, marginBottom: 16 }}>
        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr 1fr 2fr auto", gap: 10 }}>
          <input
            className="input"
            placeholder="Amount"
            value={amount}
            onChange={(e) => setAmount(e.target.value)}
          />
          <input className="input" type="date" value={date} onChange={(e) => setDate(e.target.value)} />
          <select className="input" value={type} onChange={(e) => setType(Number(e.target.value))}>
            <option value={0}>Expense</option>
            <option value={1}>Income</option>
          </select>
          <select className="input" value={categoryId} onChange={(e) => setCategoryId(e.target.value)}>
            <option value="">(no category)</option>
            {cats.map((c) => (
              <option key={c.id} value={c.id}>
                {c.name}
              </option>
            ))}
          </select>
          <button className="btn primary" onClick={add} disabled={!amount || Number(amount) <= 0}>
            Add
          </button>
        </div>

        <input
          className="input"
          placeholder="Note (optional)"
          value={note}
          onChange={(e) => setNote(e.target.value)}
        />

        {err && <div className="error">{err}</div>}
      </div>

      <div className="tableWrap">
        <table className="table">
          <thead>
            <tr>
              <th>Date</th>
              <th>Category</th>
              <th>Note</th>
              <th className="right">Amount</th>
              <th className="right">Actions</th>
            </tr>
          </thead>
          <tbody>
            {items.map((t) => (
              <tr key={t.id}>
                <td className="mono">{t.date.slice(0, 10)}</td>
                <td>{t.category?.name ?? t.categoryName ?? "-"}</td>
                <td className="muted">{t.note ?? "-"}</td>
                <td className={"right mono " + (t.type === 0 ? "neg" : "pos")}>
                  {t.type === 0 ? "-" : ""}
                  {Number(t.amount).toFixed(2)} лв.
                </td>
                <td className="right">
                  <button className="btn outlineDanger" onClick={() => del(t.id)}>
                    Delete
                  </button>
                </td>
              </tr>
            ))}

            {items.length === 0 && (
              <tr>
                <td colSpan={5} className="muted">
                  No transactions yet.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
