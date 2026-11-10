# eLogo & Logo ERP Eksik Fatura Tespit Uygulaması (Full-Stack)

eLogo entegratörü API'si üzerinden gelen e-faturaları çekip, yerel Logo ERP MS SQL veritabanındaki ETTN'ler (UUID) ile karşılaştıran ve Logo'da eksik olan faturaları tespit eden Clean Architecture tabanlı kurumsal entegrasyon sistemi.

## Teknoloji Yığını

* **Backend:** C# .NET 10 Web API (Minimal API)
* **Mimari:** Clean Architecture (Domain, Application, Infrastructure, Presentation)
* **ERP Veri Erişimi:** MS SQL Server & Dapper (Saf SQL sorguları, `NOLOCK` optimizasyonu, EF Core ve LogoRest yok)
* **Entegratör Erişimi:** eLogo API (Typed HttpClient)
* **Frontend:** Next.js (App Router), React, TypeScript, Tailwind CSS, Lucide Icons
* **Dağıtım Desteği:** Windows Service (`AddWindowsService`) ve IIS (`UseIISIntegration`) hazır

## Proje Yapısı

```
├── src/
│   ├── Core/
│   │   ├── InvoiceComparison.Domain/          # Temel Varlıklar, Recordlar, Özel Exceptionlar
│   │   └── InvoiceComparison.Application/     # İş Mantığı, DTO'lar, Servis & Repo Arayüzleri
│   ├── Infrastructure/
│   │   └── InvoiceComparison.Infrastructure/  # Dapper SQL Reposu, eLogo API İstemcisi
│   └── Presentation/
│       └── InvoiceComparison.Api/             # Minimal API Endpoints, Global Exception Handler
├── frontend/                                  # Next.js UI (Tarih Filtresi, Data Grid, PDF Modal, Toast)
├── InvoiceComparison.slnx                     # .NET 10 Solution Dosyası
└── README.md
```

## API Endpoint'leri

* `GET /api/invoices/missing?startDate=YYYY-MM-DD&endDate=YYYY-MM-DD` - Logo ERP'de olmayan eksik faturaları döner.
* `GET /api/invoices/{ettn}/pdf` - Faturanın PDF belgesini stream eder.
* `POST /api/invoices/{ettn}/mark-unread` - Faturayı entegratörde "Alınmadı" statüsüne çekerek ERP gelen kutusuna aktarır.

## Kurulum ve Çalıştırma

### 1. Backend (.NET 10 API)
```powershell
# Veritabanı ve eLogo bilgilerini appsettings.json dosyasında düzenleyin
cd src/Presentation/InvoiceComparison.Api
dotnet run
```
*API varsayılan olarak `http://localhost:5000` adresinde çalışır.*

### 2. Frontend (Next.js)
```powershell
cd frontend
npm install
npm run dev
```
*Arayüz `http://localhost:3000` adresinde açılır.*

