import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { getRecent, getSummary, getByCategory } from "../api/reports";
import type { RecentTx, Summary, ByCategoryItem } from "../api/reports";

function formatMoney(v: number) {
  const sign = v < 0 ? "-" : "";
  const abs = Math.abs(v);
  return `${sign}${abs.toFixed(2)} лв.`;
}

export default function DashboardPage() {
  const nav = useNavigate();

  const [month, setMonth] = useState(() => {
    const d = new Date();
    const m = String(d.getMonth() + 1).padStart(2, "0");
    return `${d.getFullYear()}-${m}`;
  });

  const [summary, setSummary] = useState<Summary>({ income: 0, expense: 0, balance: 0 });
  const [recent, setRecent] = useState<RecentTx[]>([]);
  const [byCategory, setByCategory] = useState<ByCategoryItem[]>([]);
  const [err, setErr] = useState("");
  const [loading, setLoading] = useState(true);

  const { year, monthNum } = useMemo(() => {
    const [y, m] = month.split("-").map(Number);
    return { year: y, monthNum: m };
  }, [month]);

  async function load() {
    setErr("");
    setLoading(true);
    try {
      const [s, r, bc] = await Promise.all([
        getSummary(year, monthNum),
        getRecent(10),
        getByCategory(year, monthNum, 0), // 0 = expenses
      ]);

      setSummary(s);
      setRecent(r);
      setByCategory(bc);
    } catch {
      setErr("Failed to load dashboard data.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [year, monthNum]);

  return (
    <div className="dash">
      <div className="dashHead">
        <div>
          <h2 className="h2">Dashboard</h2>
          <div className="muted small">Quick overview of this month</div>
        </div>

        <div className="dashActions">
          <div className="fieldInline">
            <label className="muted small">Month</label>
            <input
              className="input"
              type="month"
              value={month}
              onChange={(e) => setMonth(e.target.value)}
            />
          </div>

          <button className="btn primary" type="button" onClick={() => nav("/transactions")}>
            + Add transaction
          </button>
        </div>
      </div>

      {err && <div className="error">{err}</div>}

      <div className="statsGrid">
        <div className="statCard">
          <div className="statTop">
            <div className="statLabel">Income</div>
            <div className="pill pillGreen">This month</div>
          </div>
          <div className="statValue">{formatMoney(summary.income)}</div>
          <div className="muted small">Total income for selected month</div>
        </div>

        <div className="statCard">
          <div className="statTop">
            <div className="statLabel">Expense</div>
            <div className="pill pillRed">This month</div>
          </div>
          <div className="statValue">{formatMoney(summary.expense)}</div>
          <div className="muted small">Total spending for selected month</div>
        </div>

        <div className="statCard">
          <div className="statTop">
            <div className="statLabel">Balance</div>
            <div className="pill">Net</div>
          </div>
          <div className="statValue">{formatMoney(summary.balance)}</div>
          <div className="muted small">Income minus expense</div>
        </div>
      </div>

      <div className="grid2">
        <div className="panel">
          <div className="panelHead">
            <div>
              <div className="panelTitle">Recent transactions</div>
              <div className="muted small">Last activity</div>
            </div>
            <button className="btn" type="button" onClick={() => nav("/transactions")}>
              View all
            </button>
          </div>

          {loading ? (
            <div className="empty">
              <div className="emptyTitle">Loading...</div>
              <div className="muted small">Fetching your data</div>
            </div>
          ) : recent.length === 0 ? (
            <div className="empty">
              <div className="emptyTitle">No transactions yet</div>
              <div className="muted small">Add your first transaction to see stats here.</div>
            </div>
          ) : (
            <div className="tableWrap">
              <table className="table">
                <thead>
                  <tr>
                    <th>Date</th>
                    <th>Category</th>
                    <th>Note</th>
                    <th className="right">Amount</th>
                  </tr>
                </thead>
                <tbody>
                  {recent.map((t) => (
                    <tr key={t.id}>
                      <td className="mono">{t.date.slice(0, 10)}</td>
                      <td>{t.categoryName ?? "-"}</td>
                      <td className="muted">{t.note ?? "-"}</td>
                      <td className={"right mono " + (t.type === 0 ? "neg" : "pos")}>
                        {t.type === 0 ? "-" : ""}
                        {Number(t.amount).toFixed(2)} лв.
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>

        <div className="panel">
          <div className="panelHead">
            <div>
              <div className="panelTitle">Spending by category</div>
              <div className="muted small">This month</div>
            </div>
          </div>

          {loading ? (
            <div className="empty">
              <div className="emptyTitle">Loading...</div>
              <div className="muted small">Fetching your breakdown</div>
            </div>
          ) : byCategory.length === 0 ? (
            <div className="empty">
              <div className="emptyTitle">No expense data</div>
              <div className="muted small">Add transactions to see category breakdown.</div>
            </div>
          ) : (
            <div className="catList">
              {(() => {
                const max = Math.max(...byCategory.map((x) => Number(x.total)));
                return byCategory.slice(0, 8).map((x) => {
                  const pct = max > 0 ? Math.round((Number(x.total) / max) * 100) : 0;
                  return (
                    <div className="catRow" key={(x.categoryId ?? x.categoryName) + String(x.total)}>
                      <div className="catLeft">
                        <div className="dot" style={{ background: x.categoryColor ?? "#111827" }} />
                        <div className="catName">{x.categoryName}</div>
                      </div>

                      <div className="catBar">
                        <div className="catFill" style={{ width: `${pct}%` }} />
                      </div>

                      <div className="catValue mono">{Number(x.total).toFixed(2)} лв.</div>
                    </div>
                  );
                });
              })()}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}