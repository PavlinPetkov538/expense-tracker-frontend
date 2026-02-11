import { http } from "./http";

export async function register(email: string, fullName: string, password: string) {
  const res = await http.post("/api/Authentication/register", { email, fullName, password });
  return res.data as { token: string };
}

export async function login(email: string, password: string) {
  const res = await http.post("/api/Authentication/login", { email, password });
  return res.data as { token: string };
}

export async function me() {
  const res = await http.get("/api/Authentication/me");
  return res.data as { userId: string; email: string };
}
