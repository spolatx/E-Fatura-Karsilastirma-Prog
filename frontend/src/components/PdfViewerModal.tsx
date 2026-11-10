'use client';

import React from 'react';
import { InvoiceSummary } from '@/types/invoice';
import { X, Download, FileText, Loader2, ExternalLink } from 'lucide-react';

interface PdfViewerModalProps {
  isOpen: boolean;
  isLoading: boolean;
  pdfBlobUrl: string | null;
  invoice: InvoiceSummary | null;
  onClose: () => void;
}

export function PdfViewerModal({
  isOpen,
  isLoading,
  pdfBlobUrl,
  invoice,
  onClose,
}: PdfViewerModalProps) {
  if (!isOpen) return null;

  const handleDownload = () => {
    if (!pdfBlobUrl || !invoice) return;
    const a = document.createElement('a');
    a.href = pdfBlobUrl;
    a.download = `Invoice_${invoice.invoiceNumber || invoice.ettn}.pdf`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-3 sm:p-6 bg-slate-900/60 backdrop-blur-xs animate-in fade-in duration-200">
      <div className="bg-white rounded-2xl shadow-2xl border border-slate-200 w-full max-w-5xl h-[90vh] flex flex-col overflow-hidden animate-in zoom-in-95 duration-200">
        {/* Modal Header */}
        <div className="px-6 py-4 border-b border-slate-200 bg-slate-50 flex items-center justify-between gap-4">
          <div className="flex items-center gap-3">
            <div className="p-2 bg-blue-100 text-blue-700 rounded-lg">
              <FileText className="w-5 h-5" />
            </div>
            <div>
              <div className="flex items-center gap-2">
                <h3 className="text-sm font-bold text-slate-900">
                  {invoice?.invoiceNumber || 'E-Fatura Önizleme'}
                </h3>
                <span className="text-xs px-2 py-0.5 bg-slate-200 text-slate-700 font-semibold rounded">
                  {invoice?.currencyCode || 'TRY'}
                </span>
              </div>
              <p className="text-xs text-slate-500 truncate max-w-md">
                {invoice?.senderTitle} (VKN: {invoice?.senderTaxNumber})
              </p>
            </div>
          </div>

          <div className="flex items-center gap-2">
            {pdfBlobUrl && !isLoading && (
              <>
                <button
                  type="button"
                  onClick={handleDownload}
                  className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold text-slate-700 bg-white hover:bg-slate-100 border border-slate-300 rounded-lg transition-colors cursor-pointer"
                  title="PDF Dosyasını İndir"
                >
                  <Download className="w-4 h-4" />
                  <span className="hidden sm:inline">İndir</span>
                </button>
                <a
                  href={pdfBlobUrl}
                  target="_blank"
                  rel="noreferrer"
                  className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold text-slate-700 bg-white hover:bg-slate-100 border border-slate-300 rounded-lg transition-colors cursor-pointer"
                  title="Yeni Sekmede Aç"
                >
                  <ExternalLink className="w-4 h-4" />
                  <span className="hidden sm:inline">Yeni Sekme</span>
                </a>
              </>
            )}

            <button
              type="button"
              onClick={onClose}
              className="p-1.5 text-slate-400 hover:text-slate-700 hover:bg-slate-200 rounded-lg transition-colors cursor-pointer"
              title="Kapat"
            >
              <X className="w-5 h-5" />
            </button>
          </div>
        </div>

        {/* Modal Body: PDF Container */}
        <div className="flex-1 bg-slate-100 relative overflow-hidden flex items-center justify-center">
          {isLoading ? (
            <div className="flex flex-col items-center justify-center gap-3 text-slate-600">
              <Loader2 className="w-8 h-8 animate-spin text-blue-600" />
              <p className="text-sm font-medium">eLogo servisinden PDF yükleniyor...</p>
            </div>
          ) : pdfBlobUrl ? (
            <iframe
              src={`${pdfBlobUrl}#toolbar=1&navpanes=0`}
              title="E-Fatura PDF Görüntüleyici"
              className="w-full h-full border-none"
            />
          ) : (
            <div className="text-sm text-slate-500 font-medium">
              PDF belgesi yüklenemedi veya bulunamadı.
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

