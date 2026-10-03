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

    public const string MarketDataUnavailable =
        "urn:papertrade:error:market-data-unavailable";

    public const string Conflict =
        "urn:papertrade:error:conflict";

    public const string InsufficientFunds =
        "urn:papertrade:error:insufficient-funds";

    public const string InsufficientQuantity =
        "urn:papertrade:error:insufficient-quantity";
}
