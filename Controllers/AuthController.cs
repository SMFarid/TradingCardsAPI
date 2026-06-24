using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradingCardsAPI.Data;
using TradingCardsAPI.DTOs;
using TradingCardsAPI.Models;

namespace TradingCardsAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;

    public AuthController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            return BadRequest("Email already exists.");

        // NOTE: In production, hash the password (e.g., using BCrypt)!
        var user = new User
        {
            FullName = dto.FullName,
            Email = dto.Email,
            PasswordHash = dto.Password, // PLAIN TEXT for demo purposes only
            Role = dto.Role
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new { user.Id, user.Email, user.Role, Token = "fake-jwt-token" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null || user.PasswordHash != dto.Password)
            return Unauthorized("Invalid credentials.");

        return Ok(new { user.Id, user.Email, user.Role, Token = "fake-jwt-token" });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null)
            return NotFound("No account found with that email.");

        // Generate a simple 8-char alphanumeric token (demo only — use a secure random in production)
        user.PasswordResetToken = Guid.NewGuid().ToString("N")[..8].ToUpper();
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);

        await _context.SaveChangesAsync();

        // In production, email the token. For demo, return it directly.
        return Ok(new { message = "Reset token generated.", token = user.PasswordResetToken });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u =>
            u.PasswordResetToken == dto.Token &&
            u.PasswordResetTokenExpiry > DateTime.UtcNow);

        if (user == null)
            return BadRequest("Invalid or expired reset token.");

        // NOTE: In production, hash the password!
        user.PasswordHash = dto.NewPassword; // PLAIN TEXT for demo purposes only
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Password reset successfully." });
    }
}
