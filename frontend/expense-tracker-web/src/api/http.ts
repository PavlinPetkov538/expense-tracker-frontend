import axios from "axios";
import { getWorkspaceId } from "../state/workspace";

const API_BASE =
  import.meta.env.MODE === "development"
    ? ""
    : "https://expense-tracker-api-5rl6.onrender.com";

export const http = axios.create({
  baseURL: API_BASE,
});

http.interceptors.request.use((config) => {
  const token = sessionStorage.getItem("token");
  const wid = getWorkspaceId();

  if (wid) config.headers["X-Workspace-Id"] = wid;
  if (token) config.headers.Authorization = `Bearer ${token}`;

  return config;
});