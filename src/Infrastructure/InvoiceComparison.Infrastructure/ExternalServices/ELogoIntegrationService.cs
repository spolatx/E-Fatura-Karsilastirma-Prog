using System.Globalization;
using System.IO.Compression;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Xml.Linq;
using InvoiceComparison.Application.Interfaces;
using InvoiceComparison.Domain.Entities;
using InvoiceComparison.Domain.Enums;
using InvoiceComparison.Domain.Exceptions;
using InvoiceComparison.Infrastructure.Configuration;
using InvoiceComparison.Infrastructure.ELogoServiceReference;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InvoiceComparison.Infrastructure.ExternalServices;

public sealed class ELogoIntegrationService : IELogoIntegrationService
{
    private static readonly XNamespace UblCbcNs = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
    private static readonly XNamespace UblCacNs = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";

    private readonly ELogoOptions _options;
    private readonly ICompanyConfigService _companyConfigService;
    private readonly ILogger<ELogoIntegrationService> _logger;

    public ELogoIntegrationService(
        IOptions<ELogoOptions> options,
        ICompanyConfigService companyConfigService,
        ILogger<ELogoIntegrationService> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _companyConfigService = companyConfigService ?? throw new ArgumentNullException(nameof(companyConfigService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<InvoiceSummary>> GetInvoicesAsync(
        string companyId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(companyId);
        var company = _companyConfigService.GetCompany(companyId);

        var client = CreateClient();
        string? sessionID = null;

        try
        {
            sessionID = await LoginAsync(client, company.ELogoUserName, company.ELogoPassword, cancellationToken).ConfigureAwait(false);

            var documentType = string.IsNullOrWhiteSpace(_options.DocumentType) ? "EINVOICE" : _options.DocumentType;
            var paramList = new[]
            {
                $"DOCUMENTTYPE={documentType}",
                $"BEGINDATE={startDate:yyyy-MM-dd}",
                $"ENDDATE={endDate:yyyy-MM-dd}",
                "OPTYPE=2", // 2 = Received (Gelen / INBOX)
                "DATEBY=0"  // 0 = Belge Tarihi (Issue Date)
            };

            _logger.LogInformation(
                "Calling eLogo WCF GetDocumentList for Company '{CompanyId}' ({UserName}) between {StartDate:yyyy-MM-dd} and {EndDate:yyyy-MM-dd}.",
                company.Id,
                company.ELogoUserName,
                startDate,
                endDate);

            var request = new GetDocumentListRequest(sessionID, paramList);
            var response = await client.GetDocumentListAsync(request).ConfigureAwait(false);

            if (response?.docList is null || response.docList.Length == 0)
            {
                _logger.LogInformation("No incoming e-Invoices found on eLogo in the requested date range for company '{CompanyId}'.", company.Id);
                return [];
            }

            var invoices = new List<InvoiceSummary>(response.docList.Length);

            foreach (var doc in response.docList)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var ettn = doc.documentUuid?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(ettn))
                {
                    continue;
                }

                try
                {
                    var summary = await FetchInvoiceSummaryAsync(client, sessionID, ettn, doc.documentId, cancellationToken)
                        .ConfigureAwait(false);

                    if (summary is not null)
                    {
                        invoices.Add(summary);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "Failed to parse detailed UBL for ETTN {Ettn}, using list metadata.", ettn);

                    invoices.Add(new InvoiceSummary
                    {
                        Ettn = ettn,
                        InvoiceNumber = doc.documentId ?? string.Empty,
                        SenderTaxNumber = string.Empty,
                        SenderTitle = "e-Fatura",
                        IssueDate = startDate,
                        PayableAmount = 0m,
                        CurrencyCode = "TRY",
                        IntegrationStatus = InvoiceIntegrationStatus.ReadByErp
                    });
                }
            }

            _logger.LogInformation("Successfully retrieved {Count} incoming e-Invoices from eLogo WCF for company '{CompanyId}'.", invoices.Count, company.Id);
            return invoices;
        }
        catch (FaultException ex)
        {
            _logger.LogError(ex, "eLogo WCF Fault occurred in GetInvoicesAsync for company '{CompanyId}'.", company.Id);
            throw new ELogoApiException(
                $"eLogo WCF service fault: {ex.Message}",
                operationName: nameof(GetInvoicesAsync),
                innerException: ex);
        }
        catch (CommunicationException ex)
        {
            _logger.LogError(ex, "WCF communication error occurred while calling eLogo for company '{CompanyId}'.", company.Id);
            throw new ELogoApiException(
                $"Communication error connecting to eLogo WCF service: {ex.Message}",
                operationName: nameof(GetInvoicesAsync),
                innerException: ex);
        }
        finally
        {
            await CleanupClientAndSessionAsync(client, sessionID).ConfigureAwait(false);
        }
    }

    public async Task<byte[]> GetInvoicePdfAsync(
        string companyId,
        string ettn,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(companyId);
        ArgumentException.ThrowIfNullOrWhiteSpace(ettn);
        var company = _companyConfigService.GetCompany(companyId);

        var client = CreateClient();
        string? sessionID = null;

        try
        {
            sessionID = await LoginAsync(client, company.ELogoUserName, company.ELogoPassword, cancellationToken).ConfigureAwait(false);

            var documentType = string.IsNullOrWhiteSpace(_options.DocumentType) ? "EINVOICE" : _options.DocumentType;
            var paramList = new[]
            {
                $"DOCUMENTTYPE={documentType}",
                "DATAFORMAT=PDF",
                "DIRECTION=IN"
            };

            _logger.LogInformation("Requesting PDF document from eLogo WCF for Company '{CompanyId}', ETTN: {Ettn}", company.Id, ettn);

            var request = new GetDocumentDataRequest(sessionID, ettn.Trim(), paramList);
            var response = await client.GetDocumentDataAsync(request).ConfigureAwait(false);

            var rawBytes = response?.document?.binaryData?.Value;
            if (rawBytes is null || rawBytes.Length == 0)
            {
                throw new InvoiceNotFoundException(ettn);
            }

            return ExtractEntryFromZipIfCompressed(rawBytes, ".pdf");
        }
        catch (FaultException ex)
        {
            _logger.LogError(ex, "eLogo WCF Fault occurred in GetInvoicePdfAsync for Company '{CompanyId}', ETTN {Ettn}.", company.Id, ettn);
            if (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("bulunamadı", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvoiceNotFoundException(ettn, ex);
            }

            throw new ELogoApiException(
                $"eLogo WCF fault while fetching PDF: {ex.Message}",
                operationName: nameof(GetInvoicePdfAsync),
                innerException: ex);
        }
        catch (CommunicationException ex)
        {
            _logger.LogError(ex, "WCF communication error occurred in GetInvoicePdfAsync for Company '{CompanyId}', ETTN {Ettn}.", company.Id, ettn);
            throw new ELogoApiException(
                $"Communication error connecting to eLogo WCF service: {ex.Message}",
                operationName: nameof(GetInvoicePdfAsync),
                innerException: ex);
        }
        finally
        {
            await CleanupClientAndSessionAsync(client, sessionID).ConfigureAwait(false);
        }
    }

    public async Task<bool> MarkAsUnreadAsync(
        string companyId,
        string ettn,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(companyId);
        ArgumentException.ThrowIfNullOrWhiteSpace(ettn);
        var company = _companyConfigService.GetCompany(companyId);

        var client = CreateClient();
        string? sessionID = null;

        try
        {
            sessionID = await LoginAsync(client, company.ELogoUserName, company.ELogoPassword, cancellationToken).ConfigureAwait(false);

            var documentType = string.IsNullOrWhiteSpace(_options.DocumentType) ? "EINVOICE" : _options.DocumentType;
            var paramList = new[]
            {
                $"DOCUMENTTYPE={documentType}",
                "DIRECTION=IN",
                "STATUS=UNREAD",
                "ISMARKED=0"
            };

            _logger.LogInformation("Calling eLogo WCF GetDocumentDone to mark ETTN {Ettn} as unread for Company '{CompanyId}'.", ettn, company.Id);

            var result = await client.GetDocumentDoneAsync(sessionID, ettn.Trim(), paramList).ConfigureAwait(false);

            if (result is null)
            {
                return true;
            }

            var isSuccess = result.resultCode is 1 or 0 || result.errorCode == 0;
            if (!isSuccess && !string.IsNullOrWhiteSpace(result.resultMsg))
            {
                _logger.LogWarning(
                    "eLogo returned non-success result for Company '{CompanyId}': {ResultMsg} (Code: {Code})",
                    company.Id,
                    result.resultMsg,
                    result.resultCode);
            }

            return isSuccess;
        }
        catch (FaultException ex)
        {
            _logger.LogError(ex, "eLogo WCF Fault occurred in MarkAsUnreadAsync for Company '{CompanyId}', ETTN {Ettn}.", company.Id, ettn);
            throw new ELogoApiException(
                $"eLogo WCF fault while marking invoice as unread: {ex.Message}",
                operationName: nameof(MarkAsUnreadAsync),
                innerException: ex);
        }
        catch (CommunicationException ex)
        {
            _logger.LogError(ex, "WCF communication error occurred in MarkAsUnreadAsync for Company '{CompanyId}', ETTN {Ettn}.", company.Id, ettn);
            throw new ELogoApiException(
                $"Communication error connecting to eLogo WCF service: {ex.Message}",
                operationName: nameof(MarkAsUnreadAsync),
                innerException: ex);
        }
        finally
        {
            await CleanupClientAndSessionAsync(client, sessionID).ConfigureAwait(false);
        }
    }

    private PostBoxServiceClient CreateClient()
    {
        var endpointUrl = !string.IsNullOrWhiteSpace(_options.BaseUrl)
            ? _options.BaseUrl.Trim()
            : !string.IsNullOrWhiteSpace(_options.ServiceUrl)
                ? _options.ServiceUrl.Trim()
                : "https://pb.elogo.com.tr/PostBoxService.svc";

        var timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds > 0 ? _options.TimeoutSeconds : 90);

        Binding binding;
        if (endpointUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            binding = new BasicHttpsBinding
            {
                MaxReceivedMessageSize = 100 * 1024 * 1024,
                MaxBufferSize = 100 * 1024 * 1024,
                ReaderQuotas = System.Xml.XmlDictionaryReaderQuotas.Max,
                SendTimeout = timeout,
                ReceiveTimeout = timeout,
                OpenTimeout = TimeSpan.FromSeconds(30),
                CloseTimeout = TimeSpan.FromSeconds(30),
                Security =
                {
                    Mode = BasicHttpsSecurityMode.Transport
                }
            };
        }
        else
        {
            binding = new BasicHttpBinding
            {
                MaxReceivedMessageSize = 100 * 1024 * 1024,
                MaxBufferSize = 100 * 1024 * 1024,
                ReaderQuotas = System.Xml.XmlDictionaryReaderQuotas.Max,
                SendTimeout = timeout,
                ReceiveTimeout = timeout,
                OpenTimeout = TimeSpan.FromSeconds(30),
                CloseTimeout = TimeSpan.FromSeconds(30)
            };
        }

        var endpointAddress = new EndpointAddress(endpointUrl);
        return new PostBoxServiceClient(binding, endpointAddress);
    }

    private static async Task<string> LoginAsync(PostBoxServiceClient client, string userName, string password, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
        {
            throw new ELogoApiException(
                "eLogo credentials (UserName, Password) are not configured for this company.",
                operationName: "Login");
        }

        var loginType = new LoginType
        {
            userName = userName.Trim(),
            passWord = password.Trim(),
            appStr = "InvoiceComparisonService",
            version = "1.0"
        };

        var loginRequest = new LoginRequest(loginType);
        var loginResponse = await client.LoginAsync(loginRequest).ConfigureAwait(false);

        if (!loginResponse.LoginResult || string.IsNullOrWhiteSpace(loginResponse.sessionID))
        {
            throw new ELogoApiException(
                $"eLogo authentication failed for user '{userName}'. Please verify your Web Service credentials.",
                operationName: "Login");
        }

        return loginResponse.sessionID.Trim();
    }

    private async Task<InvoiceSummary?> FetchInvoiceSummaryAsync(
        PostBoxServiceClient client,
        string sessionID,
        string ettn,
        string? documentIdFallback,
        CancellationToken cancellationToken)
    {
        var documentType = string.IsNullOrWhiteSpace(_options.DocumentType) ? "EINVOICE" : _options.DocumentType;
        var paramList = new[]
        {
            $"DOCUMENTTYPE={documentType}",
            "DATAFORMAT=UBL",
            "DIRECTION=IN"
        };

        var request = new GetDocumentDataRequest(sessionID, ettn, paramList);
        var response = await client.GetDocumentDataAsync(request).ConfigureAwait(false);

        var rawBytes = response?.document?.binaryData?.Value;
        if (rawBytes is null || rawBytes.Length == 0)
        {
            return new InvoiceSummary
            {
                Ettn = ettn,
                InvoiceNumber = documentIdFallback ?? string.Empty,
                SenderTaxNumber = string.Empty,
                SenderTitle = "e-Fatura",
                IssueDate = DateTime.Today,
                PayableAmount = 0m,
                CurrencyCode = "TRY",
                IntegrationStatus = InvoiceIntegrationStatus.ReadByErp
            };
        }

        var ublBytes = ExtractEntryFromZipIfCompressed(rawBytes, ".xml");
        using var memoryStream = new MemoryStream(ublBytes);
        var ublDocument = await XDocument.LoadAsync(memoryStream, LoadOptions.None, cancellationToken).ConfigureAwait(false);

        return ParseUblToInvoiceSummary(ublDocument, ettn, documentIdFallback);
    }

    private static InvoiceSummary ParseUblToInvoiceSummary(XDocument ublDoc, string fallbackEttn, string? fallbackDocId)
    {
        var root = ublDoc.Root ?? throw new ELogoApiException("Invalid UBL-TR XML document: missing root element.");

        var uuid = root.Elements(UblCbcNs + "UUID").FirstOrDefault()?.Value?.Trim() ?? fallbackEttn;
        var invoiceNumber = root.Elements(UblCbcNs + "ID").FirstOrDefault()?.Value?.Trim() ?? fallbackDocId ?? string.Empty;
        var profileId = root.Elements(UblCbcNs + "ProfileID").FirstOrDefault()?.Value?.Trim() ?? string.Empty;
        var invoiceType = root.Elements(UblCbcNs + "InvoiceTypeCode").FirstOrDefault()?.Value?.Trim() ?? string.Empty;
        var currencyCode = root.Elements(UblCbcNs + "DocumentCurrencyCode").FirstOrDefault()?.Value?.Trim() ?? "TRY";

        var issueDateStr = root.Elements(UblCbcNs + "IssueDate").FirstOrDefault()?.Value?.Trim();
        var issueDate = DateTime.TryParse(issueDateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate)
            ? parsedDate
            : DateTime.Today;

        var supplierParty = root
            .Element(UblCacNs + "AccountingSupplierParty")
            ?.Element(UblCacNs + "Party");

        var senderTaxNumber = supplierParty
            ?.Elements(UblCacNs + "PartyIdentification")
            .Select(pi => pi.Element(UblCbcNs + "ID"))
            .FirstOrDefault(id => id is not null &&
                                  (string.Equals(id.Attribute("schemeID")?.Value, "VKN", StringComparison.OrdinalIgnoreCase) ||
                                   string.Equals(id.Attribute("schemeID")?.Value, "TCKN", StringComparison.OrdinalIgnoreCase) ||
                                   id.Value.Trim().Length is 10 or 11))
            ?.Value.Trim() ?? string.Empty;

        var senderTitle = supplierParty
            ?.Element(UblCacNs + "PartyName")
            ?.Element(UblCbcNs + "Name")
            ?.Value.Trim();

        if (string.IsNullOrWhiteSpace(senderTitle))
        {
            var firstName = supplierParty?.Element(UblCacNs + "Person")?.Element(UblCbcNs + "FirstName")?.Value?.Trim();
            var familyName = supplierParty?.Element(UblCacNs + "Person")?.Element(UblCbcNs + "FamilyName")?.Value?.Trim();
            senderTitle = $"{firstName} {familyName}".Trim();
        }

        var monetaryTotal = root.Element(UblCacNs + "LegalMonetaryTotal");
        var payableAmountStr = monetaryTotal?.Element(UblCbcNs + "PayableAmount")?.Value?.Trim();
        var taxExclusiveAmountStr = monetaryTotal?.Element(UblCbcNs + "TaxExclusiveAmount")?.Value?.Trim();
        var taxAmountStr = root.Element(UblCacNs + "TaxTotal")?.Element(UblCbcNs + "TaxAmount")?.Value?.Trim();

        _ = decimal.TryParse(payableAmountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var payableAmount);
        _ = decimal.TryParse(taxExclusiveAmountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var taxExclusiveAmount);
        _ = decimal.TryParse(taxAmountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var taxAmount);

        return new InvoiceSummary
        {
            Ettn = uuid,
            InvoiceNumber = invoiceNumber,
            SenderTaxNumber = senderTaxNumber,
            SenderTitle = senderTitle ?? string.Empty,
            IssueDate = issueDate,
            PayableAmount = payableAmount,
            TaxExclusiveAmount = taxExclusiveAmount,
            TaxAmount = taxAmount,
            CurrencyCode = currencyCode,
            ProfileId = profileId,
            InvoiceType = invoiceType,
            IntegrationStatus = InvoiceIntegrationStatus.ReadByErp
        };
    }

    private static byte[] ExtractEntryFromZipIfCompressed(byte[] rawBytes, string preferredExtension)
    {
        if (rawBytes.Length >= 4 &&
            rawBytes[0] == 0x50 &&
            rawBytes[1] == 0x4B &&
            rawBytes[2] == 0x03 &&
            rawBytes[3] == 0x04)
        {
            using var inputStream = new MemoryStream(rawBytes);
            using var archive = new ZipArchive(inputStream, ZipArchiveMode.Read);

            var entry = archive.Entries.FirstOrDefault(e =>
                            e.FullName.EndsWith(preferredExtension, StringComparison.OrdinalIgnoreCase))
                        ?? archive.Entries.FirstOrDefault();

            if (entry is null)
            {
                throw new ELogoApiException("eLogo returned an empty ZIP archive.");
            }

            using var entryStream = entry.Open();
            using var outputStream = new MemoryStream();
            entryStream.CopyTo(outputStream);
            return outputStream.ToArray();
        }

        return rawBytes;
    }

    private async Task CleanupClientAndSessionAsync(PostBoxServiceClient client, string? sessionID)
    {
        if (!string.IsNullOrWhiteSpace(sessionID))
        {
            try
            {
                _logger.LogInformation("Logging out eLogo WCF session: {SessionId}", sessionID);
                await client.LogoutAsync(sessionID).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Non-critical error logging out session {SessionId}.", sessionID);
            }
        }

        try
        {
            if (client.State == CommunicationState.Opened)
            {
                await client.CloseAsync().ConfigureAwait(false);
            }
            else
            {
                client.Abort();
            }
        }
        catch
        {
            client.Abort();
        }
    }
}
