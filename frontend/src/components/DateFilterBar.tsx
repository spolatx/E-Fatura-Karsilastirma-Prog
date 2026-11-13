'use client';

import React, { useEffect, useState } from 'react';
import { Calendar, Filter, RefreshCw, Info } from 'lucide-react';

interface DateFilterBarProps {
  isLoading: boolean;
  onSearch: (startDate: string, endDate: string) => void;
}

export function DateFilterBar({ isLoading, onSearch }: DateFilterBarProps) {
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');

  useEffect(() => {
    const today = new Date();
    const firstDayOfMonth = new Date(today.getFullYear(), today.getMonth(), 1);
    const formatDate = (d: Date) => d.toISOString().split('T')[0];

    setStartDate(formatDate(firstDayOfMonth));
    setEndDate(formatDate(today));
  }, []);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!startDate || !endDate) return;
    onSearch(startDate, endDate);
  };

  const setPresetRange = (preset: 'today' | 'thisWeek' | 'thisMonth' | 'lastMonth') => {
    const now = new Date();
    const formatDate = (d: Date) => d.toISOString().split('T')[0];

    let start = new Date();
    let end = new Date();

    if (preset === 'today') {
      start = now;
      end = now;
    } else if (preset === 'thisWeek') {
      const day = now.getDay() || 7; // Monday = 1
      start = new Date(now);
      start.setDate(now.getDate() - (day - 1));
      end = now;
    } else if (preset === 'thisMonth') {
      start = new Date(now.getFullYear(), now.getMonth(), 1);
      end = now;
    } else if (preset === 'lastMonth') {
      start = new Date(now.getFullYear(), now.getMonth() - 1, 1);
      end = new Date(now.getFullYear(), now.getMonth(), 0);
    }

    const s = formatDate(start);
    const e = formatDate(end);
    setStartDate(s);
    setEndDate(e);
    onSearch(s, e);
  };

  return (
    <div className="bg-white border border-slate-200 rounded-xl shadow-xs p-5">
      <form onSubmit={handleSubmit} className="flex flex-col lg:flex-row lg:items-end justify-between gap-4">
        <div className="flex flex-col sm:flex-row items-start sm:items-end gap-4 flex-1">
          {/* Start Date */}
          <div className="w-full sm:w-auto">
            <div className="flex items-center justify-between mb-1.5">
              <label htmlFor="startDate" className="block text-xs font-semibold uppercase tracking-wider text-slate-700">
                Başlangıç Tarihi
              </label>
              <span className="inline-flex items-center gap-1 text-[10px] text-blue-700 bg-blue-50 border border-blue-100 font-medium px-1.5 py-0.5 rounded ml-2" title="Filtreleme fatura kesim tarihi (IssueDate) baz alınarak yapılır">
                <Info className="w-3 h-3 text-blue-600" />
                Kesim Tarihi Bazlı
              </span>
            </div>
            <div className="relative rounded-lg border border-slate-300 focus-within:border-blue-600 focus-within:ring-2 focus-within:ring-blue-100 transition-all bg-slate-50/50">
              <input
                id="startDate"
                type="date"
                required
                value={startDate}
                onChange={(e) => setStartDate(e.target.value)}
                className="w-full py-2.5 px-3 pl-10 text-sm text-slate-800 bg-transparent rounded-lg outline-none font-medium"
              />
              <Calendar className="w-4 h-4 text-slate-400 absolute left-3 top-3.5 pointer-events-none" />
            </div>
          </div>

          {/* End Date */}
          <div className="w-full sm:w-auto">
            <div className="flex items-center justify-between mb-1.5">
              <label htmlFor="endDate" className="block text-xs font-semibold uppercase tracking-wider text-slate-700">
                Bitiş Tarihi
              </label>
              <span className="inline-flex items-center gap-1 text-[10px] text-blue-700 bg-blue-50 border border-blue-100 font-medium px-1.5 py-0.5 rounded ml-2" title="Filtreleme fatura kesim tarihi (IssueDate) baz alınarak yapılır">
                <Info className="w-3 h-3 text-blue-600" />
                Kesim Tarihi Bazlı
              </span>
            </div>
            <div className="relative rounded-lg border border-slate-300 focus-within:border-blue-600 focus-within:ring-2 focus-within:ring-blue-100 transition-all bg-slate-50/50">
              <input
                id="endDate"
                type="date"
                required
                value={endDate}
                onChange={(e) => setEndDate(e.target.value)}
                className="w-full py-2.5 px-3 pl-10 text-sm text-slate-800 bg-transparent rounded-lg outline-none font-medium"
              />
              <Calendar className="w-4 h-4 text-slate-400 absolute left-3 top-3.5 pointer-events-none" />
            </div>
          </div>

          {/* Quick Presets */}
          <div className="hidden xl:flex items-center gap-1.5 pb-1">
            <button
              type="button"
              onClick={() => setPresetRange('today')}
              className="px-2.5 py-1.5 text-xs font-medium text-slate-600 bg-slate-100 hover:bg-slate-200 rounded-md transition-colors"
            >
              Bugün
            </button>
            <button
              type="button"
              onClick={() => setPresetRange('thisWeek')}
              className="px-2.5 py-1.5 text-xs font-medium text-slate-600 bg-slate-100 hover:bg-slate-200 rounded-md transition-colors"
            >
              Bu Hafta
            </button>
            <button
              type="button"
              onClick={() => setPresetRange('thisMonth')}
              className="px-2.5 py-1.5 text-xs font-medium text-slate-600 bg-slate-100 hover:bg-slate-200 rounded-md transition-colors"
            >
              Bu Ay
            </button>
            <button
              type="button"
              onClick={() => setPresetRange('lastMonth')}
              className="px-2.5 py-1.5 text-xs font-medium text-slate-600 bg-slate-100 hover:bg-slate-200 rounded-md transition-colors"
            >
              Geçen Ay
            </button>
          </div>
        </div>

        {/* Action Button */}
        <div>
          <button
            type="submit"
            disabled={isLoading}
            className="w-full sm:w-auto inline-flex items-center justify-center gap-2 px-6 py-2.5 bg-blue-600 hover:bg-blue-700 active:bg-blue-800 text-white text-sm font-semibold rounded-lg shadow-sm hover:shadow transition-all disabled:opacity-60 disabled:cursor-not-allowed cursor-pointer"
          >
            {isLoading ? (
              <>
                <RefreshCw className="w-4 h-4 animate-spin" />
                <span>Karşılaştırılıyor...</span>
              </>
            ) : (
              <>
                <Filter className="w-4 h-4" />
                <span>Karşılaştır & Sorgula</span>
              </>
            )}
          </button>
        </div>
      </form>
    </div>
  );
}
