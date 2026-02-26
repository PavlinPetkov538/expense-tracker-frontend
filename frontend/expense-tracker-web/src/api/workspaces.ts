import { http } from "./http";

export type Workspace = { workspaceId: string; name: string; isOwner: boolean };
export type WorkspaceInvite = { token: string; workspaceId: string; workspaceName: string; expiresAt: string };

export async function getMyWorkspaces(): Promise<Workspace[]> {
  const res = await http.get("/api/workspaces/me");
  return res.data;
}

export async function createWorkspace(name: string): Promise<Workspace> {
  const res = await http.post("/api/workspaces", { name });
  return res.data;
}

export async function inviteToWorkspace(workspaceId: string, email: string): Promise<{ token: string; expiresAt: string }> {
  const res = await http.post(`/api/workspaces/${workspaceId}/invite`, { email });
  return res.data;
}

export async function getMyInvites(): Promise<WorkspaceInvite[]> {
  const res = await http.get("/api/workspaces/invites");
  return res.data;
}

export async function acceptInvite(token: string): Promise<Workspace> {
  const res = await http.post("/api/workspaces/accept", { token });
  return res.data;
}

export async function rejectInvite(token: string): Promise<void> {
  await http.post("/api/workspaces/reject", { token });
}