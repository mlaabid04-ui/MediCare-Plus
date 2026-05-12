using HospitalApp.API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SpecialtiesController : ControllerBase
{
    private readonly AppDbContext _db;
    public SpecialtiesController(AppDbContext db) { _db = db; }

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _db.Specialties
            .OrderBy(s => s.Name)
            .Select(s => new { s.Id, s.Name, s.IconName, s.Color })
            .ToListAsync());
}
