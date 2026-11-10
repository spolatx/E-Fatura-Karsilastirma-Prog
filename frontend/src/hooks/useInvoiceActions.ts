'use client';

import { useCallback, useState } from 'react';
import {
  InvoiceSummary,
  MissingInvoicesResponse,
  ToastMessage,
} from '@/types/invoice';
import {
  fetchInvoicePdfBlob,
  fetchMissingInvoices,
  markInvoiceAsUnread,
} from '@/services/invoiceApi';

export function useInvoiceActions() {
  const [data, setData] = useState<MissingInvoicesResponse | null>(null);
  const [invoices, setInvoices] = useState<InvoiceSummary[]>([]);
  const [markedEttns, setMarkedEttns] = useState<Set<string>>(new Set());

  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [actionLoadingEttn, setActionLoadingEttn] = useState<string | null>(null);

  // PDF Viewer Modal state
  const [pdfModalOpen, setPdfModalOpen] = useState<boolean>(false);
  const [pdfBlobUrl, setPdfBlobUrl] = useState<string | null>(null);
  const [pdfActiveInvoice, setPdfActiveInvoice] = useState<InvoiceSummary | null>(null);
  const [isPdfLoading, setIsPdfLoading] = useState<boolean>(false);

  // Toasts
  const [toasts, setToasts] = useState<ToastMessage[]>([]);

  const addToast = useCallback(
    (title: string, description: string, type: ToastMessage['type'] = 'info') => {
      const id = `${Date.now()}-${Math.random().toString(36).substring(2, 7)}`;
      setToasts((prev) => [...prev, { id, title, description, type }]);

      setTimeout(() => {
        setToasts((prev) => prev.filter((t) => t.id !== id));
      }, 5000);
    },
    []
  );

  const removeToast = useCallback((id: string) => {
    setToasts((prev) => prev.filter((t) => t.id !== id));
  }, []);

  const searchMissingInvoices = useCallback(
    async (startDate: string, endDate: string) => {
      setIsLoading(true);
      try {
        const result = await fetchMissingInvoices(startDate, endDate);
        setData(result);
        setInvoices(result.missingInvoices);
        setMarkedEttns(new Set());

        if (result.missingInvoices.length === 0) {
          addToast(
            'Sonuç Bulundu',
            'Seçilen tarih aralığında Logo ERP sisteminde eksik olan fatura bulunamadı. Tüm faturalar eşleşti.',
            'success'
          );
        } else {
          addToast(
            'Karşılaştırma Tamamlandı',
            `${result.missingInvoices.length} adet eksik fatura tespit edildi.`,
            'info'
          );
        }
      } catch (err: unknown) {
        const message = err instanceof Error ? err.message : 'Bilinmeyen hata';
        addToast('Sorgulama Hatası', message, 'error');
      } finally {
        setIsLoading(false);
      }
    },
    [addToast]
  );

  const openPdfViewer = useCallback(
    async (invoice: InvoiceSummary) => {
      setPdfActiveInvoice(invoice);
      setIsPdfLoading(true);
      setPdfModalOpen(true);

      // Clean up previously opened blob url
      if (pdfBlobUrl) {
        URL.revokeObjectURL(pdfBlobUrl);
        setPdfBlobUrl(null);
      }

      try {
        const blob = await fetchInvoicePdfBlob(invoice.ettn);
        const url = URL.createObjectURL(blob);
        setPdfBlobUrl(url);
      } catch (err: unknown) {
        const message = err instanceof Error ? err.message : 'PDF alınamadı';
        addToast('PDF Görüntüleme Hatası', message, 'error');
        setPdfModalOpen(false);
      } finally {
        setIsPdfLoading(false);
      }
    },
    [addToast, pdfBlobUrl]
  );

  const closePdfViewer = useCallback(() => {
    setPdfModalOpen(false);
    if (pdfBlobUrl) {
      URL.revokeObjectURL(pdfBlobUrl);
      setPdfBlobUrl(null);
    }
    setPdfActiveInvoice(null);
  }, [pdfBlobUrl]);

  const handleMarkAsUnread = useCallback(
    async (invoice: InvoiceSummary) => {
      setActionLoadingEttn(invoice.ettn);
      try {
        const result = await markInvoiceAsUnread(invoice.ettn);
        if (result.isSuccess) {
          // Visually mark row as inbox-returned and update local state
          setMarkedEttns((prev) => new Set(prev).add(invoice.ettn));

          addToast(
            'İşlem Başarılı',
            `${invoice.invoiceNumber || invoice.ettn} no'lu fatura e-fatura gelen kutusuna (Inbox) aktarıldı.`,
            'success'
          );
        } else {
          addToast('İşlem Başarısız', result.message, 'error');
        }
      } catch (err: unknown) {
        const message = err instanceof Error ? err.message : 'İşlem gerçekleştirilemedi.';
        addToast('Hata Oluştu', message, 'error');
      } finally {
        setActionLoadingEttn(null);
      }
    },
    [addToast]
  );

  return {
    data,
    invoices,
    markedEttns,
    isLoading,
    actionLoadingEttn,
    pdfModalOpen,
    pdfBlobUrl,
    pdfActiveInvoice,
    isPdfLoading,
    toasts,
    searchMissingInvoices,
    openPdfViewer,
    closePdfViewer,
    handleMarkAsUnread,
    removeToast,
  };
}

