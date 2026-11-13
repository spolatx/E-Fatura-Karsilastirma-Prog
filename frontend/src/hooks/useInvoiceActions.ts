'use client';

import { useCallback, useEffect, useState } from 'react';
import {
  CompanyItem,
  InvoiceSummary,
  MissingInvoicesResponse,
  ToastMessage,
} from '@/types/invoice';
import {
  fetchCompanies,
  fetchInvoicePdfBlob,
  fetchMissingInvoices,
  markInvoiceAsUnread,
} from '@/services/invoiceApi';

export function useInvoiceActions() {
  const [companies, setCompanies] = useState<CompanyItem[]>([]);
  const [selectedCompanyId, setSelectedCompanyId] = useState<string>('');
  const [isCompaniesLoading, setIsCompaniesLoading] = useState<boolean>(true);

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

  // Fetch companies on mount
  useEffect(() => {
    let isMounted = true;
    async function loadCompanies() {
      setIsCompaniesLoading(true);
      try {
        const list = await fetchCompanies();
        if (isMounted) {
          setCompanies(list);
          if (list.length > 0) {
            setSelectedCompanyId(list[0].id);
          }
        }
      } catch (err: unknown) {
        if (isMounted) {
          const msg = err instanceof Error ? err.message : 'Firma listesi yüklenemedi.';
          addToast('Firma Listesi Hatası', msg, 'error');
        }
      } finally {
        if (isMounted) {
          setIsCompaniesLoading(false);
        }
      }
    }

    loadCompanies();
    return () => {
      isMounted = false;
    };
  }, [addToast]);

  const searchMissingInvoices = useCallback(
    async (companyId: string, startDate: string, endDate: string) => {
      if (!companyId) {
        addToast('Firma Seçilmedi', 'Lütfen işlem yapmak istediğiniz firmayı seçin.', 'error');
        return;
      }

      setIsLoading(true);
      try {
        const result = await fetchMissingInvoices(companyId, startDate, endDate);
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
      if (!selectedCompanyId) {
        addToast('Hata', 'Firma bilgisi bulunamadı.', 'error');
        return;
      }

      setPdfActiveInvoice(invoice);
      setIsPdfLoading(true);
      setPdfModalOpen(true);

      // Clean up previously opened blob url
      if (pdfBlobUrl) {
        URL.revokeObjectURL(pdfBlobUrl);
        setPdfBlobUrl(null);
      }

      try {
        const blob = await fetchInvoicePdfBlob(selectedCompanyId, invoice.ettn);
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
    [addToast, pdfBlobUrl, selectedCompanyId]
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
      if (!selectedCompanyId) {
        addToast('Hata', 'Firma bilgisi bulunamadı.', 'error');
        return;
      }

      setActionLoadingEttn(invoice.ettn);
      try {
        const result = await markInvoiceAsUnread(selectedCompanyId, invoice.ettn);
        if (result.isSuccess) {
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
    [addToast, selectedCompanyId]
  );

  return {
    companies,
    selectedCompanyId,
    setSelectedCompanyId,
    isCompaniesLoading,
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
