namespace FitRos.API.Contracts.Auth;

public sealed record ResetPasswordHttpRequest(
    string Email,
    string Token,
    string NewPassword);