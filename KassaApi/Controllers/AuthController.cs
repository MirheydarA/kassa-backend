using System.Security.Claims;
using KassaApi.Data;
using KassaApi.DTOs;
using KassaApi.Services;
using Microsoft.AspNetCore.Authorization;
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

    [Authorize]
    [HttpPost("change-password")]
    public async Task<ActionResult> ChangePassword(ChangePasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
            return BadRequest(new { message = "Yeni şifrə ən azı 6 simvol olmalıdır" });

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);
        var user = _db.Users.FirstOrDefault(u => u.Id == userId);
        if (user == null)
            return Unauthorized();

        if (!_auth.VerifyPassword(user, request.CurrentPassword))
            return BadRequest(new { message = "Cari şifrə yanlışdır" });

        user.PasswordHash = _auth.HashPassword(user, request.NewPassword);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Şifrə uğurla dəyişdirildi" });
    }
}
