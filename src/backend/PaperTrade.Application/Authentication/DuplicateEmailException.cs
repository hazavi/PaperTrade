namespace PaperTrade.Application.Authentication;

public sealed class DuplicateEmailException(Exception innerException)
    : Exception(
        "A user with this email already exists.",
        innerException);
