using HospitalApp.API.Data;
using HospitalApp.API.Models;
using Microsoft.EntityFrameworkCore;

namespace HospitalApp.API.Services;

public interface IDocumentService
{
    Task<List<PatientDocumentDto>> GetPatientDocumentsAsync(Guid patientId);
    Task<PatientDocumentDto?> UploadDocumentAsync(Guid patientId, IFormFile file, string category);
    Task<bool> DeleteDocumentAsync(Guid documentId, Guid patientId);
}

public class DocumentService : IDocumentService
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public DocumentService(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public async Task<List<PatientDocumentDto>> GetPatientDocumentsAsync(Guid patientId)
    {
        var docs = await _db.PatientDocuments
            .Where(d => d.PatientId == patientId)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();

        return docs.Select(d => ToDto(d)).ToList();
    }

    public async Task<PatientDocumentDto?> UploadDocumentAsync(Guid patientId, IFormFile file, string category)
    {
        var folder = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "documents", patientId.ToString());
        Directory.CreateDirectory(folder);

        var ext = Path.GetExtension(file.FileName);
        var storedName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(folder, storedName);

        await using (var stream = File.Create(filePath))
            await file.CopyToAsync(stream);

        var doc = new PatientDocument
        {
            PatientId    = patientId,
            OriginalName = file.FileName,
            StoredName   = storedName,
            ContentType  = file.ContentType,
            FileSize     = file.Length,
            Category     = category,
            UploadedAt   = DateTime.UtcNow
        };

        _db.PatientDocuments.Add(doc);
        await _db.SaveChangesAsync();
        return ToDto(doc);
    }

    public async Task<bool> DeleteDocumentAsync(Guid documentId, Guid patientId)
    {
        var doc = await _db.PatientDocuments
            .FirstOrDefaultAsync(d => d.Id == documentId && d.PatientId == patientId);
        if (doc == null) return false;

        var filePath = Path.Combine(
            _env.WebRootPath ?? "wwwroot", "uploads", "documents",
            patientId.ToString(), doc.StoredName);
        if (File.Exists(filePath)) File.Delete(filePath);

        _db.PatientDocuments.Remove(doc);
        await _db.SaveChangesAsync();
        return true;
    }

    private static PatientDocumentDto ToDto(PatientDocument d) => new()
    {
        Id           = d.Id,
        OriginalName = d.OriginalName,
        ContentType  = d.ContentType,
        FileSize     = d.FileSize,
        Category     = d.Category,
        UploadedAt   = d.UploadedAt,
        FileUrl      = $"/uploads/documents/{d.PatientId}/{d.StoredName}"
    };
}

public class PatientDocumentDto
{
    public Guid Id { get; set; }
    public string OriginalName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long FileSize { get; set; }
    public string Category { get; set; } = "Autre";
    public DateTime UploadedAt { get; set; }
    public string FileUrl { get; set; } = "";
}
