import { http } from "./http";

export type Transaction = {
  id: string;
  userId: string;
  amount: number;
  date: string;
  type: number;
  note?: string | null;
  categoryId?: string | null;
  categoryName?: string | null;

  category?: { id: string; name: string; color?: string | null } | null;

  createdAt: string;
};

export type GetTransactionsParams = {
  take?: number;
  from?: string;       // "YYYY-MM-DD"
  to?: string;         // "YYYY-MM-DD"
  type?: number;       // 0/1
  categoryId?: string; // guid
  q?: string;
};

export async function getTransactions(params: GetTransactionsParams = {}) {
  const res = await http.get("/api/transactions", { params });
  return res.data as Transaction[];
}
export type SearchTransactionsParams = {
  take?: number;
  categoryName?: string;
  createdFrom?: string; // "YYYY-MM-DD"
  createdTo?: string;   // "YYYY-MM-DD"
};

export async function searchTransactions(params: SearchTransactionsParams = {}) {
  const res = await http.get("/api/transactions/search", { params });
  return res.data as Transaction[];
}


export async function createTransaction(payload: {
  amount: number;
  date: string; // "YYYY-MM-DD"
  type: number;
  note?: string;
  categoryId?: string | null;
}) {
  const res = await http.post("/api/transactions", payload);
  return res.data as Transaction;
}

export async function deleteTransaction(id: string) {
  await http.delete(`/api/transactions/${id}`);
}
