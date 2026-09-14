using KassaApi.Data;
using KassaApi.DTOs;
using KassaApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KassaApi.Controllers;

[ApiController]
[Route("api/clients")]
[Authorize]
public class ClientsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ClientsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<ClientDto>>> GetAll([FromQuery] string? search)
    {
        var query = _db.Clients.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.Name.Contains(search));

        var clients = await query.OrderBy(c => c.Name)
            .Select(c => new ClientDto { Id = c.Id, Name = c.Name, Phone = c.Phone })
            .ToListAsync();

        return Ok(clients);
    }

    [HttpPost]
    public async Task<ActionResult<ClientDto>> Create(CreateClientRequest request)
    {
        var client = new Client { Name = request.Name.Trim(), Phone = request.Phone };
        _db.Clients.Add(client);
        await _db.SaveChangesAsync();
        return Ok(new ClientDto { Id = client.Id, Name = client.Name, Phone = client.Phone });
    }

    // Adı ilə tapır, yoxdursa yaradır (Loan/MyDebt/Exchange formalarında istifadə üçün)
    public static async Task<Client> FindOrCreateAsync(AppDbContext db, string name)
    {
        var trimmed = name.Trim();
        var client = await db.Clients.FirstOrDefaultAsync(c => c.Name == trimmed);
        if (client == null)
        {
            client = new Client { Name = trimmed };
            db.Clients.Add(client);
            await db.SaveChangesAsync();
        }
        return client;
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ClientDto>> Update(int id, UpdateClientRequest request)
    {
        var client = await _db.Clients.FindAsync(id);
        if (client == null) return NotFound();

        client.Name = request.Name.Trim();
        client.Phone = request.Phone;
        await _db.SaveChangesAsync();

        return Ok(new ClientDto { Id = client.Id, Name = client.Name, Phone = client.Phone });
    }
}
