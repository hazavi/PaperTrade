using System.Security.Claims;

namespace PaperTrade.Api.Extensions;

internal static class ClaimsPrincipalExtensions
{
    public static bool TryGetUserId(
        this ClaimsPrincipal principal,
        out Guid userId)
    {
        return Guid.TryParse(
            principal.FindFirstValue(ClaimTypes.NameIdentifier),
            out userId);
    }
}
