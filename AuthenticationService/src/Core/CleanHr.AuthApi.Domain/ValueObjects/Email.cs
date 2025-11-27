using System.Collections.Generic;
using System.Text.RegularExpressions;
using CleanHr.AuthApi.Domain.Primitives;

namespace CleanHr.AuthApi.Domain.ValueObjects;

public sealed class Email : ValueObject
{
    private const int _maxLength = 50;

    private Email(string value)
    {
        Value = value;
    }

    public string Value { get; private set; }

    public static Result<Email> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<Email>.Failure("Email", "The Email cannot be null or empty.");
        }

        if (value.Length > _maxLength)
        {
            return Result<Email>.Failure("Email", $"The Email length must be less than {_maxLength + 1} characters.");
        }

        Regex emailRegex = new(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$");

        Match match = emailRegex.Match(value);

        if (match.Success == false)
        {
            return Result<Email>.Failure("Email", "The Email value is not a valid email.");
        }

        return Result<Email>.Success(new Email(value));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
