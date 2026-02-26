import { http } from "./http";

export type AiCreatedTransaction = {
  id: string;
  amount: number;
  date: string;
  type: number;
  category?: string;
  note?: string;
  confidence?: number;
};

export async function aiAddFromText(text: string): Promise<AiCreatedTransaction> {
  const res = await http.post("/api/ai/transactions/from-text", { text });
  return res.data;
}

export async function aiAddFromReceipt(file: File, extraNote?: string): Promise<AiCreatedTransaction> {
  const fd = new FormData();
  fd.append("file", file);
  if (extraNote) fd.append("extraNote", extraNote);
  const res = await http.post("/api/ai/transactions/from-receipt", fd, {
    headers: { "Content-Type": "multipart/form-data" },
  });
  return res.data;
}