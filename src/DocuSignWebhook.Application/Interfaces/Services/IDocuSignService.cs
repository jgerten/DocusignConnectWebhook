using DocuSignWebhook.Domain.Entities;

namespace DocuSignWebhook.Application.Interfaces.Services;

/// <summary>
/// Service for interacting with DocuSign API
/// </summary>
public interface IDocuSignService
{
    /// <summary>
    /// Downloads a document from a completed DocuSign envelope
    /// </summary>
    /// <param name="envelopeId">DocuSign envelope ID</param>
    /// <param name="documentId">DocuSign document ID</param>
    /// <returns>Document content as byte array</returns>
    Task<byte[]> DownloadDocumentAsync(string envelopeId, string documentId);

    /// <summary>
    /// Gets envelope details from DocuSign
    /// </summary>
    /// <param name="envelopeId">DocuSign envelope ID</param>
    /// <returns>Envelope entity with metadata</returns>
    Task<Envelope> GetEnvelopeDetailsAsync(string envelopeId);

    /// <summary>
    /// Lists all documents in an envelope
    /// </summary>
    /// <param name="envelopeId">DocuSign envelope ID</param>
    /// <returns>List of document metadata</returns>
    Task<List<Document>> ListEnvelopeDocumentsAsync(string envelopeId);

    /// <summary>
    /// Downloads all documents from an envelope as a combined PDF
    /// </summary>
    /// <param name="envelopeId">DocuSign envelope ID</param>
    /// <returns>Combined PDF as byte array</returns>
    Task<byte[]> DownloadAllDocumentsAsync(string envelopeId);
}
