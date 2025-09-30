namespace TutBackend.Models;

public class AuthResponseDto
{
    public string ErrorMessage { get; set; } = string.Empty;
    public bool IsAuthenticated { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresOn { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime RefreshTokenExpiration { get; set; }
}
