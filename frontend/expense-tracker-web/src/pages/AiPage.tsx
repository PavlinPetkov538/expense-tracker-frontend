import { useState } from "react";
import { aiAddFromReceipt, aiAddFromText } from "../api/ai";

export default function AiPage() {
  const [mode, setMode] = useState<"text" | "receipt">("receipt");
  const [text, setText] = useState("");
  const [file, setFile] = useState<File | null>(null);
  const [extra, setExtra] = useState("");
  const [busy, setBusy] = useState(false);
  const [err, setErr] = useState("");
  const [result, setResult] = useState<any>(null);

  async function submit() {
    setErr("");
    setResult(null);
    setBusy(true);
    try {
      if (mode === "text") {
        if (!text.trim()) throw new Error("Write something first.");
        const r = await aiAddFromText(text.trim());
        setResult(r);
        setText("");
      } else {
        if (!file) throw new Error("Choose an image or PDF receipt.");
        const r = await aiAddFromReceipt(file, extra.trim() || undefined);
        setResult(r);
        setFile(null);
        setExtra("");
      }
    } catch (e: any) {
      setErr(e?.response?.data ?? e?.message ?? "Failed.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="panel">
      <div className="panelHead">
        <div>
          <div className="panelTitle">AI Add</div>
          <div className="muted small">Add an expense or income from text or a receipt</div>
        </div>
      </div>

      <div style={{ display: "flex", gap: 8, marginBottom: 12 }}>
        <button className={mode === "receipt" ? "btn primary" : "btn"} onClick={() => setMode("receipt")}>
          Receipt (image/PDF)
        </button>
        <button className={mode === "text" ? "btn primary" : "btn"} onClick={() => setMode("text")}>
          Text
        </button>
      </div>

      {mode === "text" ? (
        <div style={{ display: "grid", gap: 10 }}>
          <textarea
            className="input"
            rows={5}
            placeholder='Example: "Bought groceries for 23.50 yesterday"'
            value={text}
            onChange={(e) => setText(e.target.value)}
          />
        </div>
      ) : (
        <div style={{ display: "grid", gap: 10 }}>
          <input
            className="input"
            type="file"
            accept="image/*,application/pdf"
            onChange={(e) => setFile(e.target.files?.[0] || null)}
          />
          <input
            className="input"
            placeholder="Optional note (e.g., paid by cash, split, etc.)"
            value={extra}
            onChange={(e) => setExtra(e.target.value)}
          />
        </div>
      )}

      <div style={{ display: "flex", gap: 10, marginTop: 12, alignItems: "center" }}>
        <button className="btn primary" onClick={submit} disabled={busy}>
          {busy ? "Working..." : "AI Add"}
        </button>
        {err && <div className="error">{String(err)}</div>}
      </div>

      {result && (
        <div style={{ marginTop: 14 }} className="panel">
          <div className="muted small">Created transaction</div>
          <pre style={{ whiteSpace: "pre-wrap" }}>{JSON.stringify(result, null, 2)}</pre>
        </div>
      )}
    </div>
  );
}