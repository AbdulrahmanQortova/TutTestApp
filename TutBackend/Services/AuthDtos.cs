using System.Text.Json.Serialization;

namespace TutBackend.Services;
    // Request DTOs
    public sealed class RegisterRequest
    {
        [JsonPropertyName("username")] public string Username { get; init; } = null!;
        [JsonPropertyName("password")] public string Password { get; init; } = null!;
        [JsonPropertyName("role")] public string Role { get; init; } = null!;
    }

    public sealed class LoginRequest
    {
        [JsonPropertyName("username")] public string Username { get; init; } = null!;
        [JsonPropertyName("password")] public string Password { get; init; } = null!;
        [JsonPropertyName("role")] public string Role { get; init; } = null!;
    }

    public sealed class RefreshRequest
    {
        [JsonPropertyName("refreshToken")] public string RefreshToken { get; init; } = null!;
    }

    public sealed class ValidateRequest
    {
        [JsonPropertyName("token")] public string Token { get; init; } = null!;
    }

    // Response DTOs
    public sealed class TokenResponse
    {
        [JsonPropertyName("accessToken")] public string AccessToken { get; init; } = null!;
        [JsonPropertyName("refreshToken")] public string RefreshToken { get; init; } = null!;
        [JsonPropertyName("expiresAt")] public System.DateTimeOffset ExpiresAt { get; init; }
    }

    public sealed class ValidateResponse
    {
        [JsonPropertyName("isValid")] public bool IsValid { get; init; }
        [JsonPropertyName("username")] public string? Username { get; init; }
        [JsonPropertyName("role")] public string? Role { get; init; }
        [JsonPropertyName("expiresAt")] public System.DateTimeOffset? ExpiresAt { get; init; }
    }
