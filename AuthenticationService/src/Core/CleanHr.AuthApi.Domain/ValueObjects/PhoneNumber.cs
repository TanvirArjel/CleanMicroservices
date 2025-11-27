using System.Collections.Generic;
using CleanHr.AuthApi.Domain.Primitives;

namespace CleanHr.AuthApi.Domain.ValueObjects;

public sealed class PhoneNumber : ValueObject
{
    private const int _minLength = 10;
    private const int _maxLength = 20;

    private PhoneNumber(string value)
    {
        Value = value;
    }

    public string Value { get; private set; }

    public static Result<PhoneNumber> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<PhoneNumber>.Failure("PhoneNumber", "The PhoneNumber value cannot be null or empty.");
        }

        if (value.Length < _minLength || value.Length > _maxLength)
        {
            return Result<PhoneNumber>.Failure("PhoneNumber", $"The PhoneNumber value must be in between {_minLength} && {_maxLength} characters.");
        }

        return Result<PhoneNumber>.Success(new PhoneNumber(value));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}
