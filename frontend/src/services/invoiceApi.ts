import {
  MarkInvoiceUnreadResponse,
  MissingInvoicesResponse,
  ProblemDetails,
} from '@/types/invoice';

const API_BASE_URL = (
  process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000'
).replace(/\/$/, '');

export async function fetchMissingInvoices(
  startDate: string,
  endDate: string,
  signal?: AbortSignal
): Promise<MissingInvoicesResponse> {
  const url = `${API_BASE_URL}/api/invoices/missing?startDate=${encodeURIComponent(
    startDate
  )}&endDate=${encodeURIComponent(endDate)}`;

  const response = await fetch(url, {
    method: 'GET',
    headers: {
      Accept: 'application/json',
    },
    signal,
  });

  if (!response.ok) {
    const errorBody = (await response.json().catch(() => null)) as ProblemDetails | null;
    const errorMessage =
      errorBody?.detail ||
      errorBody?.title ||
      `Server error: ${response.status} ${response.statusText}`;
    throw new Error(errorMessage);
  }

  return response.json();
}

export async function fetchInvoicePdfBlob(
  ettn: string,
  signal?: AbortSignal
): Promise<Blob> {
  const url = `${API_BASE_URL}/api/invoices/${encodeURIComponent(ettn)}/pdf`;

  const response = await fetch(url, {
    method: 'GET',
    headers: {
      Accept: 'application/pdf',
    },
    signal,
  });

  if (!response.ok) {
    const errorBody = (await response.json().catch(() => null)) as ProblemDetails | null;
    const errorMessage =
      errorBody?.detail ||
      errorBody?.title ||
      `Failed to download PDF: ${response.status} ${response.statusText}`;
    throw new Error(errorMessage);
  }

  return response.blob();
}

export async function markInvoiceAsUnread(
  ettn: string,
  signal?: AbortSignal
): Promise<MarkInvoiceUnreadResponse> {
  const url = `${API_BASE_URL}/api/invoices/${encodeURIComponent(
    ettn
  )}/mark-unread`;

  const response = await fetch(url, {
    method: 'POST',
    headers: {
      Accept: 'application/json',
    },
    signal,
  });

  if (!response.ok) {
    const errorBody = (await response.json().catch(() => null)) as ProblemDetails | null;
    const errorMessage =
      errorBody?.detail ||
      errorBody?.title ||
      `Failed to mark invoice as unread: ${response.status} ${response.statusText}`;
    throw new Error(errorMessage);
  }

  return response.json();
}

