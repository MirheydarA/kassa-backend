using KassaApi.Data;
using KassaApi.DTOs;
using KassaApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace KassaApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly AuthService _auth;

    public AuthController(AppDbContext db, AuthService auth)
    {
        _db = db;
        _auth = auth;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var user = _db.Users.FirstOrDefault(u => u.Username == request.Username);
        if (user == null || !_auth.VerifyPassword(user, request.Password))
            return Unauthorized(new { message = "İstifadəçi adı və ya şifrə yanlışdır" });

        var (token, expiresAt) = _auth.GenerateToken(user);
        return Ok(new LoginResponse { Token = token, Username = user.Username, ExpiresAt = expiresAt });
    }
}
