using System.Text.Json;
using FluentValidation.Results;

namespace PaperTrade.Api.Extensions;

internal static class ValidationResultExtensions
{
    public static Dictionary<string, string[]> ToErrorDictionary(
        this ValidationResult validationResult)
    {
        return validationResult.Errors
            .GroupBy(error =>
                JsonNamingPolicy.CamelCase.ConvertName(
                    error.PropertyName))
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(error => error.ErrorMessage)
                    .Distinct()
                    .ToArray());
    }
}