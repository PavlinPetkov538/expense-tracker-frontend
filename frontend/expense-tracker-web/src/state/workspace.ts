export const WORKSPACE_ID_KEY = "workspaceId";
export const WORKSPACE_NAME_KEY = "workspaceName";

export function getWorkspaceId(): string | null {
  return localStorage.getItem(WORKSPACE_ID_KEY);
}

export function setWorkspace(id: string, name?: string) {
  localStorage.setItem(WORKSPACE_ID_KEY, id);
  if (name) localStorage.setItem(WORKSPACE_NAME_KEY, name);
}

export function getWorkspaceName(): string | null {
  return localStorage.getItem(WORKSPACE_NAME_KEY);
}