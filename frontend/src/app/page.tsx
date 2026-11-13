'use client';

import React from 'react';
import { useInvoiceActions } from '@/hooks/useInvoiceActions';
import { DateFilterBar } from '@/components/DateFilterBar';
import { SummaryKpiCards } from '@/components/SummaryKpiCards';
import { MissingInvoiceGrid } from '@/components/MissingInvoiceGrid';
import { PdfViewerModal } from '@/components/PdfViewerModal';
import { ToastNotification } from '@/components/ToastNotification';
import { Layers, Database, ShieldCheck } from 'lucide-react';

export default function HomePage() {
  const {
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
  } = useInvoiceActions();

  return (
    <div className="min-h-screen bg-slate-100/75 text-slate-900 flex flex-col font-sans">
      {/* Top Application Navbar */}
      <header className="bg-white border-b border-slate-200 sticky top-0 z-30 shadow-2xs">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 h-16 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-blue-600 text-white flex items-center justify-center shadow-xs">
              <Layers className="w-5 h-5" />
            </div>
            <div>
              <h1 className="text-base sm:text-lg font-bold text-slate-900 leading-tight flex items-center gap-2">
                <span>eLogo &amp; Logo ERP Karşılaştırma</span>
                <span className="text-[10px] font-semibold px-2 py-0.5 rounded-md bg-blue-50 text-blue-700 border border-blue-200 uppercase tracking-wider">
                  v1.0 Pro
                </span>
              </h1>
              <p className="text-xs text-slate-500">
                Entegratör ve Logo MS SQL Veritabanı Eksik E-Fatura Denetimi
              </p>
            </div>
          </div>

          <div className="hidden md:flex items-center gap-4 text-xs font-medium text-slate-600">
            <div className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-slate-50 border border-slate-200">
              <Database className="w-3.5 h-3.5 text-blue-600" />
              <span>Dapper / Direct SQL</span>
            </div>
            <div className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-slate-50 border border-slate-200">
              <ShieldCheck className="w-3.5 h-3.5 text-emerald-600" />
              <span>Clean Architecture</span>
            </div>
          </div>
        </div>
      </header>

      {/* Main Content Dashboard */}
      <main className="flex-1 max-w-7xl w-full mx-auto px-4 sm:px-6 lg:px-8 py-6 space-y-6">
        {/* Date Filter & Trigger Bar */}
        <DateFilterBar
          companies={companies}
          selectedCompanyId={selectedCompanyId}
          onCompanyChange={setSelectedCompanyId}
          isCompaniesLoading={isCompaniesLoading}
          isLoading={isLoading}
          onSearch={searchMissingInvoices}
        />

        {/* Comparison KPI Statistics Cards */}
        <SummaryKpiCards data={data} />

        {/* Data Grid Component */}
        <MissingInvoiceGrid
          invoices={invoices}
          markedEttns={markedEttns}
          actionLoadingEttn={actionLoadingEttn}
          onOpenPdf={openPdfViewer}
          onMarkAsUnread={handleMarkAsUnread}
        />
      </main>

      {/* Footer */}
      <footer className="border-t border-slate-200 bg-white py-4 text-center text-xs text-slate-500">
        <p>eLogo &amp; Logo ERP Entegrasyon Servisi &bull; .NET 10 &amp; Next.js</p>
      </footer>

      {/* PDF Viewer Dialog Modal */}
      <PdfViewerModal
        isOpen={pdfModalOpen}
        isLoading={isPdfLoading}
        pdfBlobUrl={pdfBlobUrl}
        invoice={pdfActiveInvoice}
        onClose={closePdfViewer}
      />

      {/* Toast / Snackbar Notifications Container */}
      <ToastNotification
        toasts={toasts}
        onDismiss={removeToast}
      />
    </div>
  );
}
