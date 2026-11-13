export type InvoiceIntegrationStatus =
  | 'unknown'
  | 'unreadInInbox'
  | 'readByErp'
  | 'archived'
  | 'cancelled';

export interface CompanyItem {
  id: string;
  name: string;
}

export interface InvoiceSummary {
  ettn: string;
  invoiceNumber: string;
  senderTaxNumber: string;
  senderTitle: string;
  issueDate: string;
  payableAmount: number;
  taxExclusiveAmount: number;
  taxAmount: number;
  currencyCode: string;
  profileId: string;
  invoiceType: string;
  integrationStatus: InvoiceIntegrationStatus;
}

export interface MissingInvoicesResponse {
  startDate: string;
  endDate: string;
  totalIntegratorInvoicesCount: number;
  totalErpMatchedCount: number;
  missingInvoicesCount: number;
  missingInvoices: InvoiceSummary[];
}

export interface MarkInvoiceUnreadResponse {
  ettn: string;
  isSuccess: boolean;
  message: string;
  processedAtUtc: string;
}

export interface ProblemDetails {
  status?: number;
  title?: string;
  detail?: string;
  instance?: string;
  errorCode?: string;
  traceId?: string;
}

export interface ToastMessage {
  id: string;
  title: string;
  description: string;
  type: 'success' | 'error' | 'info';
}
