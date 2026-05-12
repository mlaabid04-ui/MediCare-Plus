using HospitalApp.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _service;
    public DocumentsController(IDocumentService svc) { _service = svc; }

    [HttpGet("patient/{patientId}")]
    public async Task<IActionResult> GetAll(Guid patientId)
        => Ok(await _service.GetPatientDocumentsAsync(patientId));

    [HttpPost("patient/{patientId}")]
    [RequestSizeLimit(20 * 1024 * 1024)] // 20 MB
    public async Task<IActionResult> Upload(Guid patientId, IFormFile file, [FromForm] string category = "Autre")
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Aucun fichier fourni." });

        var allowed = new[] { "application/pdf", "image/jpeg", "image/png", "image/jpg",
                               "image/webp", "application/msword",
                               "application/vnd.openxmlformats-officedocument.wordprocessingml.document" };
        if (!allowed.Contains(file.ContentType))
            return BadRequest(new { message = "Type de fichier non autorisé." });

        var result = await _service.UploadDocumentAsync(patientId, file, category);
        return result == null ? StatusCode(500) : Ok(result);
    }

    [HttpDelete("{id}/patient/{patientId}")]
    public async Task<IActionResult> Delete(Guid id, Guid patientId)
    {
        var ok = await _service.DeleteDocumentAsync(id, patientId);
        return ok ? Ok() : NotFound();
    }
}
