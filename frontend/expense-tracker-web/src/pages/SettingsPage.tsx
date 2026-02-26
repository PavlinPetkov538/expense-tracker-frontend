import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  acceptInvite,
  createWorkspace,
  getMyInvites,
  getMyWorkspaces,
  inviteToWorkspace,
  rejectInvite,
  type Workspace,
  type WorkspaceInvite,
} from "../api/workspaces";
import { getWorkspaceId, getWorkspaceName, setWorkspace } from "../state/workspace";

export default function SettingsPage() {
  const nav = useNavigate();

  const [workspaces, setWorkspaces] = useState<Workspace[]>([]);
  const [invites, setInvites] = useState<WorkspaceInvite[]>([]);
  const [err, setErr] = useState("");
  const [busy, setBusy] = useState(false);

  const [newFamilyName, setNewFamilyName] = useState("");
  const [inviteEmail, setInviteEmail] = useState("");

  const currentWorkspaceId = getWorkspaceId();
  const currentWorkspaceName = getWorkspaceName();

  const current = useMemo(() => workspaces.find((w) => w.workspaceId === currentWorkspaceId) ?? null, [
    workspaces,
    currentWorkspaceId,
  ]);

  async function load() {
    setErr("");
    try {
      const [ws, inv] = await Promise.all([getMyWorkspaces(), getMyInvites()]);
      setWorkspaces(ws);
      setInvites(inv);

      const saved = getWorkspaceId();
      if (!saved) {
        const personal = ws.find((x) => x.name.toLowerCase() === "personal") ?? ws[0];
        if (personal) setWorkspace(personal.workspaceId, personal.name);
      }
    } catch {
      setErr("Failed to load settings.");
    }
  }

  useEffect(() => {
    load();
  }, []);

  function logout() {
    sessionStorage.removeItem("token");
    nav("/login");
  }

  async function onSwitch(id: string) {
    const ws = workspaces.find((w) => w.workspaceId === id);
    if (!ws) return;
    setWorkspace(ws.workspaceId, ws.name);
    window.location.reload();
  }

  async function onCreateFamily() {
    setErr("");
    setBusy(true);
    try {
      const name = newFamilyName.trim();
      if (name.length < 2) throw new Error("Name too short.");
      const created = await createWorkspace(name);
      setNewFamilyName("");
      await load();
      setWorkspace(created.workspaceId, created.name);
      window.location.reload();
    } catch (e: any) {
      setErr(e?.response?.data ?? e?.message ?? "Failed to create workspace.");
    } finally {
      setBusy(false);
    }
  }

  async function onInvite() {
    setErr("");
    setBusy(true);
    try {
      if (!current) throw new Error("Select a workspace first.");
      if (!current.isOwner) throw new Error("Only the owner can invite members.");
      const email = inviteEmail.trim();
      if (!email.includes("@")) throw new Error("Invalid email.");
      await inviteToWorkspace(current.workspaceId, email);
      setInviteEmail("");
      alert("Invite created. The invited user can accept it in Settings > Invitations.");
    } catch (e: any) {
      setErr(e?.response?.data ?? e?.message ?? "Failed to invite.");
    } finally {
      setBusy(false);
    }
  }

  async function onAccept(token: string) {
    setErr("");
    setBusy(true);
    try {
      const ws = await acceptInvite(token);
      await load();
      setWorkspace(ws.workspaceId, ws.name);
      window.location.reload();
    } catch (e: any) {
      setErr(e?.response?.data ?? e?.message ?? "Failed to accept invite.");
    } finally {
      setBusy(false);
    }
  }

  async function onReject(token: string) {
    setErr("");
    setBusy(true);
    try {
      await rejectInvite(token);
      await load();
    } catch (e: any) {
      setErr(e?.response?.data ?? e?.message ?? "Failed to reject invite.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="panel">
      <h2 style={{ margin: "0 0 14px 0" }}>Settings</h2>

      <div style={{ display: "grid", gap: 14 }}>
        <div className="row">
          <div>
            <div style={{ fontWeight: 600 }}>Current account</div>
            <div className="muted small">Switch between Personal and Family accounts</div>
          </div>
          <select
            className="input"
            style={{ width: 260 }}
            value={currentWorkspaceId ?? ""}
            onChange={(e) => onSwitch(e.target.value)}
          >
            <option value="" disabled>
              Select account
            </option>
            {workspaces.map((w) => (
              <option key={w.workspaceId} value={w.workspaceId}>
                {w.name} {w.isOwner ? "(Owner)" : ""}
              </option>
            ))}
          </select>
        </div>

        <div className="panel" style={{ padding: 14 }}>
          <div style={{ fontWeight: 700, marginBottom: 8 }}>Family / Master account</div>
          <div className="muted small" style={{ marginBottom: 10 }}>
            Create a Family account and invite members. The person who invites is the owner (master).
          </div>

          <div style={{ display: "grid", gap: 10 }}>
            <div style={{ display: "grid", gridTemplateColumns: "1fr auto", gap: 10 }}>
              <input
                className="input"
                placeholder="Family account name (e.g., Petkov Family)"
                value={newFamilyName}
                onChange={(e) => setNewFamilyName(e.target.value)}
              />
              <button className="btn primary" onClick={onCreateFamily} disabled={busy || newFamilyName.trim().length < 2}>
                Create
              </button>
            </div>

            <div style={{ display: "grid", gridTemplateColumns: "1fr auto", gap: 10 }}>
              <input
                className="input"
                placeholder="Invite email"
                value={inviteEmail}
                onChange={(e) => setInviteEmail(e.target.value)}
                disabled={!currentWorkspaceId}
              />
              <button className="btn" onClick={onInvite} disabled={busy || !inviteEmail.trim()}>
                Invite
              </button>
            </div>

            <div className="muted small">
              Selected: <b>{current?.name ?? currentWorkspaceName ?? "—"}</b> {current?.isOwner ? "(Owner)" : ""}
            </div>
          </div>
        </div>

        <div className="panel" style={{ padding: 14 }}>
          <div style={{ fontWeight: 700, marginBottom: 8 }}>Invitations</div>
          <div className="muted small" style={{ marginBottom: 10 }}>
            Invitations sent to your email will show here. You can accept or reject.
          </div>

          {invites.length === 0 ? (
            <div className="muted small">No pending invitations.</div>
          ) : (
            <div style={{ display: "grid", gap: 8 }}>
              {invites.map((i) => (
                <div key={i.token} className="row" style={{ alignItems: "center" }}>
                  <div>
                    <div style={{ fontWeight: 600 }}>{i.workspaceName}</div>
                    <div className="muted small">Expires: {new Date(i.expiresAt).toLocaleString()}</div>
                  </div>
                  <div style={{ display: "flex", gap: 8 }}>
                    <button className="btn primary" onClick={() => onAccept(i.token)} disabled={busy}>
                      Accept
                    </button>
                    <button className="btn outlineDanger" onClick={() => onReject(i.token)} disabled={busy}>
                      Reject
                    </button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        {err && <div className="error">{err}</div>}

        <div className="row">
          <div style={{ color: "var(--muted)", fontSize: 14 }}>Sign out from this session</div>
          <button className="btn outlineDanger" onClick={logout}>
            Logout
          </button>
        </div>
      </div>
    </div>
  );
}