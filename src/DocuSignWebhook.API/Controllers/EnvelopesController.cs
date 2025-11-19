using DocuSignWebhook.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DocuSignWebhook.API.Controllers;

/// <summary>
/// Controller for managing envelopes and documents
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class EnvelopesController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<EnvelopesController> _logger;

    public EnvelopesController(
        IApplicationDbContext context,
        ILogger<EnvelopesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Gets all envelopes
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetEnvelopes([FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        var envelopes = await _context.Envelopes
            .Where(e => e.IsActive)
            .OrderByDescending(e => e.CreatedAt)
            .Skip(skip)
            .Take(take)
            .Include(e => e.Documents)
            .ToListAsync();

        return Ok(envelopes);
    }

    /// <summary>
    /// Gets a specific envelope by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetEnvelope(Guid id)
    {
        var envelope = await _context.Envelopes
            .Include(e => e.Documents)
            .Include(e => e.WebhookEvents)
            .FirstOrDefaultAsync(e => e.Id == id && e.IsActive);

        if (envelope == null)
            return NotFound();

        return Ok(envelope);
    }

    /// <summary>
    /// Gets envelope by DocuSign envelope ID
    /// </summary>
    [HttpGet("docusign/{docusignEnvelopeId}")]
    public async Task<IActionResult> GetEnvelopeByDocuSignId(string docusignEnvelopeId)
    {
        var envelope = await _context.Envelopes
            .Include(e => e.Documents)
            .Include(e => e.WebhookEvents)
            .FirstOrDefaultAsync(e => e.DocuSignEnvelopeId == docusignEnvelopeId && e.IsActive);

        if (envelope == null)
            return NotFound();

        return Ok(envelope);
    }

    /// <summary>
    /// Gets documents for a specific envelope
    /// </summary>
    [HttpGet("{id}/documents")]
    public async Task<IActionResult> GetEnvelopeDocuments(Guid id)
    {
        var documents = await _context.Documents
            .Where(d => d.EnvelopeId == id && d.IsActive)
            .OrderBy(d => d.Order)
            .ToListAsync();

        return Ok(documents);
    }
}
