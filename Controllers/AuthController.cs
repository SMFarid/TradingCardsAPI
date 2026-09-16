using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradingCardsAPI.Data;
using TradingCardsAPI.DTOs;
using TradingCardsAPI.Models;
using TradingCardsAPI.Services;

namespace TradingCardsAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly TokenService _tokenService;

    public AuthController(AppDbContext context, TokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            return BadRequest("Email already exists.");

        if (dto.Password.Length < 6)
            return BadRequest("Password must be at least 6 characters.");

        var user = new User
        {
            FullName = dto.FullName,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = dto.Role
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(AuthResponse(user));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null || !await VerifyPasswordAsync(user, dto.Password))
            return Unauthorized("Invalid credentials.");

        return Ok(AuthResponse(user));
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null)
            return NotFound("No account found with that email.");

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

        if (dto.NewPassword.Length < 6)
            return BadRequest("Password must be at least 6 characters.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Password reset successfully." });
    }

    /// <summary>
    /// Verifies against the BCrypt hash. Accounts created before hashing was
    /// introduced stored plaintext; on a successful legacy match the password
    /// is transparently upgraded to a BCrypt hash.
    /// </summary>
    private async Task<bool> VerifyPasswordAsync(User user, string password)
    {
        if (user.PasswordHash.StartsWith("$2"))
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            }
            catch (BCrypt.Net.SaltParseException)
            {
                return false;
            }
        }

        if (user.PasswordHash == password)
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            await _context.SaveChangesAsync();
            return true;
        }
        return false;
    }

    private object AuthResponse(User user) => new
    {
        user.Id,
        user.Email,
        user.Role,
        user.FullName,
        Token = _tokenService.CreateToken(user)
    };
}
