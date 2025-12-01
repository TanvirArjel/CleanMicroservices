using System.Diagnostics;

namespace CleanHr.AuthApi.Application.Telemetry;

/// <summary>
/// Provides ActivitySource for application-level distributed tracing.
/// </summary>
public static class ApplicationDiagnostics
{
    public const string ActivitySourceName = "CleanHr.AuthApi.Application";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName, "1.0.0");

    /// <summary>
    /// Activity names for different operations
    /// </summary>
    public static class Activities
    {
        public const string UserLogin = "user.login";
        public const string UserRegistration = "user.registration";
        public const string TokenGeneration = "token.generation";
        public const string PasswordValidation = "password.validation";
        public const string EmailVerification = "email.verification";
        public const string PasswordReset = "password.reset";
    }

    /// <summary>
    /// Semantic conventions for activity tags
    /// </summary>
    public static class Tags
    {
        public const string UserId = "user.id";
        public const string Email = "user.email";
        public const string UserName = "user.name";
        public const string Operation = "operation.name";
        public const string Result = "operation.result";
        public const string ErrorType = "error.type";
        public const string ErrorMessage = "error.message";
    }
}
