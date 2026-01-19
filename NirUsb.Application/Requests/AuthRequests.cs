namespace NirUsb.Application.Requests;

public static class AuthRequests {
    public record RegisterRequest(
        string Name,
        string Password
    );

    public record LoginRequest(
        string Name,
        string Password
    );
}