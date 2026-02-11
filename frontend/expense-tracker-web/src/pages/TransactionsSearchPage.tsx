import { useEffect, useMemo, useState } from "react";
import { searchTransactions } from "../api/transactions";
import type { Transaction } from "../api/transactions";

export default function TransactionsSearchPage() {
  const [items, setItems] = useState<Transaction[]>([]);
  const [err, setErr] = useState("");
  const [loading, setLoading] = useState(false);

  const [categoryName, setCategoryName] = useState("");
  const [createdFrom, setCreatedFrom] = useState<string>("");
  const [createdTo, setCreatedTo] = useState<string>("");

  const params = useMemo(() => {
    const p: any = { take: 200 };
    if (categoryName.trim()) p.categoryName = categoryName.trim();
    if (createdFrom) p.createdFrom = createdFrom;
    if (createdTo) p.createdTo = createdTo;
    return p;
  }, [categoryName, createdFrom, createdTo]);

  async function load() {
    setErr("");
    setLoading(true);
    try {
      setItems(await searchTransactions(params));
    } catch {
      setErr("Failed to load results.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    const id = setTimeout(load, 250);
    return () => clearTimeout(id);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [params]);

  return (
    <div className="panel">
      <div className="panelHead">
        <div>
          <div className="panelTitle">Search</div>
          <div className="muted small">Search by category name + created date</div>
        </div>
      </div>

      <div className="filterBar">
        <div className="filterRow">
          <input
            className="input"
            placeholder="Category name..."
            value={categoryName}
            onChange={(e) => setCategoryName(e.target.value)}
          />
          <input className="input" type="date" value={createdFrom} onChange={(e) => setCreatedFrom(e.target.value)} />
          <input className="input" type="date" value={createdTo} onChange={(e) => setCreatedTo(e.target.value)} />
        </div>
      </div>

      {err && <div className="error">{err}</div>}

      {loading ? (
        <div className="empty">
          <div className="emptyTitle">Loading...</div>
          <div className="muted small">Searching transactions</div>
        </div>
      ) : (
        <div className="tableWrap">
          <table className="table">
            <thead>
              <tr>
                <th>Created</th>
                <th>Category</th>
                <th>Note</th>
                <th className="right">Amount</th>
              </tr>
            </thead>
            <tbody>
              {items.map((t) => (
                <tr key={t.id}>
                  <td className="mono">{t.createdAt?.slice?.(0, 10) ?? "-"}</td>
                  <td>{t.category?.name ?? t.categoryName ?? "-"}</td>
                  <td className="muted">{t.note ?? "-"}</td>
                  <td className={"right mono " + (t.type === 0 ? "neg" : "pos")}>
                    {t.type === 0 ? "-" : ""}
                    {Number(t.amount).toFixed(2)} лв.
                  </td>
                </tr>
              ))}

              {items.length === 0 && (
                <tr>
                  <td colSpan={4} className="muted">No results.</td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
