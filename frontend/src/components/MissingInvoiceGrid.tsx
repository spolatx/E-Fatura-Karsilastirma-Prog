'use client';

import React, { useMemo, useState } from 'react';
import { InvoiceSummary } from '@/types/invoice';
import {
  FileSearch,
  Inbox,
  Search,
  CheckCircle,
  Copy,
  Loader2,
  ArrowDown,
  ArrowUp,
  ArrowUpDown,
} from 'lucide-react';

interface MissingInvoiceGridProps {
  invoices: InvoiceSummary[];
  markedEttns: Set<string>;
  actionLoadingEttn: string | null;
  onOpenPdf: (invoice: InvoiceSummary) => void;
  onMarkAsUnread: (invoice: InvoiceSummary) => void;
}

export function MissingInvoiceGrid({
  invoices,
  markedEttns,
  actionLoadingEttn,
  onOpenPdf,
  onMarkAsUnread,
}: MissingInvoiceGridProps) {
  const [searchTerm, setSearchTerm] = useState('');
  const [copiedEttn, setCopiedEttn] = useState<string | null>(null);
  const [sortDirection, setSortDirection] = useState<'desc' | 'asc'>('desc');

  const toggleSort = () => {
    setSortDirection((prev) => (prev === 'desc' ? 'asc' : 'desc'));
  };

  const filteredInvoices = useMemo(() => {
    let list = invoices;
    if (searchTerm.trim()) {
      const term = searchTerm.toLowerCase();
      list = list.filter(
        (inv) =>
          inv.invoiceNumber?.toLowerCase().includes(term) ||
          inv.senderTitle?.toLowerCase().includes(term) ||
          inv.senderTaxNumber?.toLowerCase().includes(term) ||
          inv.ettn?.toLowerCase().includes(term)
      );
    }

    return [...list].sort((a, b) => {
      const timeA = new Date(a.issueDate).getTime() || 0;
      const timeB = new Date(b.issueDate).getTime() || 0;
      if (timeA !== timeB) {
        return sortDirection === 'desc' ? timeB - timeA : timeA - timeB;
      }
      return (a.invoiceNumber || '').localeCompare(b.invoiceNumber || '', undefined, { sensitivity: 'base' });
    });
  }, [invoices, searchTerm, sortDirection]);

  const copyToClipboard = (text: string) => {
    navigator.clipboard.writeText(text);
    setCopiedEttn(text);
    setTimeout(() => setCopiedEttn(null), 2000);
  };

  const formatDate = (dateStr: string) => {
    if (!dateStr) return '-';
    try {
      const d = new Date(dateStr);
      return d.toLocaleDateString('tr-TR', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
      });
    } catch {
      return dateStr;
    }
  };

  const formatAmount = (val: number, currency: string) => {
    return (
      new Intl.NumberFormat('tr-TR', {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
      }).format(val) + ` ${currency || 'TRY'}`
    );
  };

  return (
    <div className="bg-white border border-slate-200 rounded-xl shadow-xs overflow-hidden">
      {/* Table Header & Search Bar */}
      <div className="p-4 sm:p-5 border-b border-slate-200 bg-slate-50/50 flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-4">
        <div>
          <h2 className="text-base font-bold text-slate-800 flex items-center gap-2">
            <span>Logo ERP&apos;de Eksik Faturalar</span>
            <span className="text-xs font-semibold px-2 py-0.5 rounded-full bg-amber-100 text-amber-800">
              {filteredInvoices.length} Kayıt
            </span>
          </h2>
          <p className="text-xs text-slate-500 mt-0.5">
            Faturalar kesim tarihine göre en yeniden eskiye doğru sıralanmaktadır.
          </p>
        </div>

        {/* Quick Search */}
        <div className="relative w-full sm:w-72">
          <input
            type="text"
            placeholder="Unvan, VKN, Fatura No, ETTN ara..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="w-full py-2 pl-9 pr-3 text-xs text-slate-800 bg-white border border-slate-300 rounded-lg outline-none focus:border-blue-500 focus:ring-1 focus:ring-blue-500"
          />
          <Search className="w-4 h-4 text-slate-400 absolute left-2.5 top-2.5 pointer-events-none" />
        </div>
      </div>

      {/* Table Container */}
      <div className="overflow-x-auto">
        <table className="w-full text-left text-xs border-collapse">
          <thead>
            <tr className="bg-slate-100/75 border-b border-slate-200 text-slate-600 font-semibold uppercase tracking-wider">
              <th className="py-3 px-4">
                <button
                  type="button"
                  onClick={toggleSort}
                  className="inline-flex items-center gap-1 font-semibold uppercase hover:text-blue-600 cursor-pointer"
                  title="Kesim Tarihine göre sırala"
                >
                  <span>Kesim Tarihi</span>
                  {sortDirection === 'desc' ? (
                    <ArrowDown className="w-3.5 h-3.5 text-blue-600" />
                  ) : (
                    <ArrowUp className="w-3.5 h-3.5 text-blue-600" />
                  )}
                </button>
              </th>
              <th className="py-3 px-4">Fatura No</th>
              <th className="py-3 px-4">Gönderen Unvan</th>
              <th className="py-3 px-4">VKN / TCKN</th>
              <th className="py-3 px-4 text-right">Tutar</th>
              <th className="py-3 px-4">Para Birimi</th>
              <th className="py-3 px-4">ETTN (UUID)</th>
              <th className="py-3 px-4 text-center">Durum</th>
              <th className="py-3 px-4 text-right">Aksiyonlar</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100">
            {filteredInvoices.length === 0 ? (
              <tr>
                <td colSpan={9} className="py-12 text-center text-slate-400">
                  <div className="flex flex-col items-center justify-center gap-2">
                    <CheckCircle className="w-8 h-8 text-emerald-500" />
                    <p className="text-sm font-medium text-slate-600">
                      Gösterilecek eksik fatura bulunamadı.
                    </p>
                    <p className="text-xs text-slate-400">
                      Tarih aralığını kontrol edebilir veya yeni bir sorgulama yapabilirsiniz.
                    </p>
                  </div>
                </td>
              </tr>
            ) : (
              filteredInvoices.map((inv) => {
                const isMarked = markedEttns.has(inv.ettn);
                const isActionLoading = actionLoadingEttn === inv.ettn;

                return (
                  <tr
                    key={inv.ettn}
                    className={`transition-colors hover:bg-slate-50 ${
                      isMarked ? 'bg-emerald-50/40 hover:bg-emerald-50/60' : ''
                    }`}
                  >
                    {/* Issue Date */}
                    <td className="py-3.5 px-4 font-semibold text-slate-800 whitespace-nowrap">
                      {formatDate(inv.issueDate)}
                    </td>

                    {/* Invoice Number */}
                    <td className="py-3.5 px-4 whitespace-nowrap">
                      <span className="font-semibold text-slate-900 bg-slate-100 px-2 py-0.5 rounded border border-slate-200">
                        {inv.invoiceNumber || '-'}
                      </span>
                    </td>

                    {/* Sender Title */}
                    <td className="py-3.5 px-4 text-slate-800 font-medium max-w-xs truncate" title={inv.senderTitle}>
                      {inv.senderTitle || '-'}
                    </td>

                    {/* Sender Tax Number */}
                    <td className="py-3.5 px-4 font-mono text-slate-600 whitespace-nowrap">
                      {inv.senderTaxNumber || '-'}
                    </td>

                    {/* Payable Amount */}
                    <td className="py-3.5 px-4 text-right font-bold text-slate-900 whitespace-nowrap">
                      {formatAmount(inv.payableAmount, inv.currencyCode)}
                    </td>

                    {/* Currency */}
                    <td className="py-3.5 px-4 whitespace-nowrap">
                      <span className="text-[11px] font-semibold text-slate-600 bg-slate-100 px-1.5 py-0.5 rounded">
                        {inv.currencyCode || 'TRY'}
                      </span>
                    </td>

                    {/* ETTN */}
                    <td className="py-3.5 px-4 whitespace-nowrap">
                      <div className="flex items-center gap-1.5 font-mono text-[11px] text-slate-500">
                        <span title={inv.ettn}>
                          {inv.ettn.substring(0, 8)}...{inv.ettn.substring(inv.ettn.length - 4)}
                        </span>
                        <button
                          type="button"
                          onClick={() => copyToClipboard(inv.ettn)}
                          title="ETTN Kopyala"
                          className="p-1 hover:text-slate-900 text-slate-400 rounded transition-colors"
                        >
                          {copiedEttn === inv.ettn ? (
                            <CheckCircle className="w-3.5 h-3.5 text-emerald-600" />
                          ) : (
                            <Copy className="w-3.5 h-3.5" />
                          )}
                        </button>
                      </div>
                    </td>

                    {/* Status Badge */}
                    <td className="py-3.5 px-4 text-center whitespace-nowrap">
                      {isMarked ? (
                        <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[11px] font-medium bg-emerald-100 text-emerald-800 border border-emerald-200">
                          <CheckCircle className="w-3 h-3" />
                          Inbox&apos;a Alındı
                        </span>
                      ) : (
                        <span className="inline-flex items-center px-2 py-0.5 rounded-full text-[11px] font-medium bg-rose-100 text-rose-800 border border-rose-200">
                          Logo&apos;da Yok
                        </span>
                      )}
                    </td>

                    {/* Row Actions */}
                    <td className="py-3.5 px-4 text-right whitespace-nowrap">
                      <div className="inline-flex items-center gap-2">
                        {/* View PDF Button */}
                        <button
                          type="button"
                          onClick={() => onOpenPdf(inv)}
                          className="inline-flex items-center gap-1.5 px-2.5 py-1.5 text-xs font-semibold text-slate-700 bg-white border border-slate-300 hover:bg-slate-50 active:bg-slate-100 rounded-md shadow-2xs transition-all cursor-pointer"
                          title="Fatura PDF belgesini modal pencerede incele"
                        >
                          <FileSearch className="w-3.5 h-3.5 text-blue-600" />
                          <span>İncele (PDF)</span>
                        </button>

                        {/* Mark As Unread Button */}
                        <button
                          type="button"
                          disabled={isMarked || isActionLoading}
                          onClick={() => onMarkAsUnread(inv)}
                          className={`inline-flex items-center gap-1.5 px-2.5 py-1.5 text-xs font-semibold rounded-md shadow-2xs transition-all cursor-pointer ${
                            isMarked
                              ? 'bg-slate-100 text-slate-400 border border-slate-200 cursor-not-allowed'
                              : 'text-amber-800 bg-amber-50 hover:bg-amber-100 active:bg-amber-200 border border-amber-300'
                          }`}
                          title="Entegratör üzerinde faturayı 'Alınmadı' statüsüne çekerek ERP gelen kutusuna düşürür"
                        >
                          {isActionLoading ? (
                            <Loader2 className="w-3.5 h-3.5 animate-spin" />
                          ) : (
                            <Inbox className="w-3.5 h-3.5 text-amber-700" />
                          )}
                          <span>
                            {isMarked ? 'İşlendi' : 'Alınmadı Olarak İşaretle'}
                          </span>
                        </button>
                      </div>
                    </td>
                  </tr>
                );
              })
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
