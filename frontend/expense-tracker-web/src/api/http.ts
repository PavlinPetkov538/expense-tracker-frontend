import axios from "axios";

export const http = axios.create({ baseURL: "/" });

http.interceptors.request.use((config) => {
  const token = sessionStorage.getItem("token");
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});