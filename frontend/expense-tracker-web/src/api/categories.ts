import { http } from "./http";

export type Category = {
  id: string;
  userId: string;
  name: string;
  type: number;
  color?: string | null;
  createdAt: string;
};

export async function getCategories() {
  const res = await http.get("/api/categories");
  return res.data as Category[];
}

export async function createCategory(name: string, type: number, color?: string) {
  const res = await http.post("/api/categories", { name, type, color: color || null });
  return res.data as Category;
}

export async function deleteCategory(id: string) {
  await http.delete(`/api/categories/${id}`);
}