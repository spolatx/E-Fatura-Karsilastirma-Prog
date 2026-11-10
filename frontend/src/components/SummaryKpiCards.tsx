'use client';

import React from 'react';
import { MissingInvoicesResponse } from '@/types/invoice';
import { CheckCircle2, AlertTriangle, FileText, Banknote } from 'lucide-react';

interface SummaryKpiCardsProps {
  data: MissingInvoicesResponse | null;
}

export function SummaryKpiCards({ data }: SummaryKpiCardsProps) {
  if (!data) return null;

  const totalMissingAmount = data.missingInvoices.reduce(
    (sum, inv) => sum + (inv.payableAmount || 0),
    0
  );

  const formatCurrency = (val: number) => {
    return new Intl.NumberFormat('tr-TR', {
      style: 'currency',
      currency: 'TRY',
      minimumFractionDigits: 2,
    }).format(val);
  };

  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
      {/* Total Integrator */}
      <div className="bg-white border border-slate-200 rounded-xl p-4 shadow-xs flex items-center gap-4">
        <div className="p-3 bg-blue-50 text-blue-600 rounded-lg">
          <FileText className="w-6 h-6" />
        </div>
        <div>
          <p className="text-xs font-semibold uppercase tracking-wider text-slate-500">
            eLogo Gelen Fatura
          </p>
          <p className="text-2xl font-bold text-slate-800">
            {data.totalIntegratorInvoicesCount}
          </p>
        </div>
      </div>

      {/* ERP Matched */}
      <div className="bg-white border border-slate-200 rounded-xl p-4 shadow-xs flex items-center gap-4">
        <div className="p-3 bg-emerald-50 text-emerald-600 rounded-lg">
          <CheckCircle2 className="w-6 h-6" />
        </div>
        <div>
          <p className="text-xs font-semibold uppercase tracking-wider text-slate-500">
            Logo ERP Eşleşen
          </p>
          <p className="text-2xl font-bold text-emerald-600">
            {data.totalErpMatchedCount}
          </p>
        </div>
      </div>

      {/* Missing In ERP */}
      <div className="bg-white border border-slate-200 rounded-xl p-4 shadow-xs flex items-center gap-4">
        <div className="p-3 bg-amber-50 text-amber-600 rounded-lg">
          <AlertTriangle className="w-6 h-6" />
        </div>
        <div>
          <p className="text-xs font-semibold uppercase tracking-wider text-slate-500">
            Logo&apos;da Eksik Fatura
          </p>
          <p className="text-2xl font-bold text-amber-600">
            {data.missingInvoicesCount}
          </p>
        </div>
      </div>

      {/* Missing Total Amount */}
      <div className="bg-white border border-slate-200 rounded-xl p-4 shadow-xs flex items-center gap-4">
        <div className="p-3 bg-indigo-50 text-indigo-600 rounded-lg">
          <Banknote className="w-6 h-6" />
        </div>
        <div>
          <p className="text-xs font-semibold uppercase tracking-wider text-slate-500">
            Eksik Faturalar Toplamı
          </p>
          <p className="text-xl font-bold text-slate-800">
            {formatCurrency(totalMissingAmount)}
          </p>
        </div>
      </div>
    </div>
  );
}

