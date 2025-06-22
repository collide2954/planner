using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;

namespace Server.Services;

public class AuthService
{
    private readonly PlannerDbContext _context;
    private readonly ILogger<AuthService> _logger;

    public AuthService(PlannerDbContext context, ILogger<AuthService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> RegisterUser(string username, string password)
    {
        try
        {
            if (await _context.Users.AnyAsync(u => u.Username == username))
            {
                _logger.LogWarning("Registration attempted with existing username: {Username}", username);
                return false;
            }

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

            var user = new User
            {
                Username = username,
                PasswordHash = passwordHash
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("User registered successfully: {Username}", username);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user: {Username}", username);
            return false;
        }
    }

    public async Task<bool> ValidateUser(string username, string password)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
            {
                _logger.LogWarning("Login attempted with non-existent username: {Username}", username);
                return false;
            }

            bool isValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            _logger.LogInformation("Login attempt for {Username}: {Result}", username, isValid ? "Success" : "Failed");
            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating user: {Username}", username);
            return false;
        }
    }
}
