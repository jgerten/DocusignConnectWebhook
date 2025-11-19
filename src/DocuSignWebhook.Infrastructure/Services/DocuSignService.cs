using DocuSign.eSign.Api;
using DocuSign.eSign.Client;
using DocuSign.eSign.Model;
using DocuSignWebhook.Application.Interfaces.Services;
using DocuSignWebhook.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace DocuSignWebhook.Infrastructure.Services;

/// <summary>
/// Implementation of DocuSign API integration
/// </summary>
public class DocuSignService : IDocuSignService
{
    private readonly ILogger<DocuSignService> _logger;
    private readonly string _accountId;
    private readonly string _accessToken;
    private readonly string _basePath;
    private readonly ApiClient _apiClient;

    public DocuSignService(
        ILogger<DocuSignService> logger,
        string accountId,
        string accessToken,
        string basePath = "https://demo.docusign.net/restapi")
    {
        _logger = logger;
        _accountId = accountId;
        _accessToken = accessToken;
        _basePath = basePath;

        // Initialize DocuSign API client
        _apiClient = new ApiClient(_basePath);
        _apiClient.Configuration.DefaultHeader.Add("Authorization", $"Bearer {_accessToken}");
    }

    public async Task<byte[]> DownloadDocumentAsync(string envelopeId, string documentId)
    {
        try
        {
            var envelopesApi = new EnvelopesApi(_apiClient);
            var document = await envelopesApi.GetDocumentAsync(_accountId, envelopeId, documentId);

            using var memoryStream = new MemoryStream();
            await document.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading document {DocumentId} from envelope {EnvelopeId}",
                documentId, envelopeId);
            throw;
        }
    }

    public async Task<Domain.Entities.Envelope> GetEnvelopeDetailsAsync(string envelopeId)
    {
        try
        {
            var envelopesApi = new EnvelopesApi(_apiClient);
            var docusignEnvelope = await envelopesApi.GetEnvelopeAsync(_accountId, envelopeId);

            var envelope = new Domain.Entities.Envelope
            {
                DocuSignEnvelopeId = docusignEnvelope.EnvelopeId,
                Subject = docusignEnvelope.EmailSubject ?? "No Subject",
                Status = docusignEnvelope.Status ?? "unknown",
                SenderEmail = docusignEnvelope.Sender?.Email ?? "unknown",
                SenderName = docusignEnvelope.Sender?.UserName ?? "unknown",
                SentAt = ParseDateTime(docusignEnvelope.SentDateTime),
                CompletedAt = ParseDateTime(docusignEnvelope.CompletedDateTime),
                VoidedAt = ParseDateTime(docusignEnvelope.VoidedDateTime),
                VoidedReason = docusignEnvelope.VoidedReason
            };

            return envelope;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting envelope details for {EnvelopeId}", envelopeId);
            throw;
        }
    }

    public async Task<List<Domain.Entities.Document>> ListEnvelopeDocumentsAsync(string envelopeId)
    {
        try
        {
            var envelopesApi = new EnvelopesApi(_apiClient);
            var documentsResult = await envelopesApi.ListDocumentsAsync(_accountId, envelopeId);

            var documents = new List<Domain.Entities.Document>();

            foreach (var doc in documentsResult.EnvelopeDocuments ?? new List<EnvelopeDocument>())
            {
                // Skip the "certificate" document - it's metadata
                if (doc.DocumentId == "certificate")
                    continue;

                var docName = doc.Name ?? $"document_{doc.DocumentId}";
                var fileExtension = "pdf"; // Default to PDF
                if (docName.Contains('.'))
                {
                    fileExtension = Path.GetExtension(docName).TrimStart('.');
                }

                var document = new Domain.Entities.Document
                {
                    DocuSignDocumentId = doc.DocumentId,
                    Name = docName,
                    DocumentType = doc.Type ?? "pdf",
                    FileExtension = fileExtension,
                    Order = int.Parse(doc.Order ?? "0"),
                    PageCount = doc.Pages?.Count
                };

                documents.Add(document);
            }

            return documents;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing documents for envelope {EnvelopeId}", envelopeId);
            throw;
        }
    }

    public async Task<byte[]> DownloadAllDocumentsAsync(string envelopeId)
    {
        try
        {
            var envelopesApi = new EnvelopesApi(_apiClient);

            // Use "combined" as documentId to get all documents in one PDF
            var document = await envelopesApi.GetDocumentAsync(_accountId, envelopeId, "combined");

            using var memoryStream = new MemoryStream();
            await document.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading combined documents from envelope {EnvelopeId}", envelopeId);
            throw;
        }
    }

    private DateTime? ParseDateTime(string? dateTimeString)
    {
        if (string.IsNullOrEmpty(dateTimeString))
            return null;

        if (DateTime.TryParse(dateTimeString, out var result))
            return result;

        return null;
    }
}
