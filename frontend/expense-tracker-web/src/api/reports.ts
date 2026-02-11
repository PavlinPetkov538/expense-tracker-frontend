import { http } from "./http";

export type Summary = {
  income: number;
  expense: number;
  balance: number;
};

export type RecentTx = {
  id: string;
  date: string;
  type: number; // 0 expense, 1 income
  amount: number;
  note?: string | null;
  categoryId?: string | null;
  categoryName?: string | null;
};

export async function getSummary(year: number, month: number) {
  const res = await http.get("/api/reports/summary", { params: { year, month } });
  return res.data as Summary;
}

export async function getRecent(take = 10) {
  const res = await http.get("/api/reports/recent", { params: { take } });
  return res.data as RecentTx[];
}

export type ByCategoryItem = {
  categoryId?: string | null;
  categoryName: string;
  categoryColor?: string | null;
  total: number;
};

export async function getByCategory(year: number, month: number, type = 0) {
  const res = await http.get("/api/reports/by-category", { params: { year, month, type } });
  return res.data as ByCategoryItem[];
}
