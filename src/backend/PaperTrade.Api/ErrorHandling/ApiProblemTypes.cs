namespace PaperTrade.Api.ErrorHandling;

internal static class ApiProblemTypes
{
    public const string Validation =
        "urn:papertrade:error:validation";

    public const string InvalidCredentials =
        "urn:papertrade:error:invalid-credentials";

    public const string DuplicateEmail =
        "urn:papertrade:error:duplicate-email";

    public const string Unauthorized =
        "urn:papertrade:error:unauthorized";

    public const string NotFound =
        "urn:papertrade:error:not-found";

    public const string InternalServerError =
        "urn:papertrade:error:internal-server-error";
}