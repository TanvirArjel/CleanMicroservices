using System.Collections.Generic;
using CleanHr.DepartmentApi.Domain.Exceptions;
using CleanHr.DepartmentApi.Domain.Primitives;

namespace CleanHr.DepartmentApi.Domain.ValueObjects;

public sealed class DepartmentName : ValueObject
{
    private const int _minLength = 5;

    private const int _maxLength = 50;

    public DepartmentName(string value)
    {
        SetValue(value);
    }

    public string Value { get; private set; }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    private void SetValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException(DepartmentDomainErrors.NameNullOrEmpty);
        }

        if (value.Length < _minLength || value.Length > _maxLength)
        {
            string errorMessage = DepartmentDomainErrors.GetNameLengthOutOfRangeErrorMessage(_minLength, _maxLength);
            throw new DomainValidationException(errorMessage);
        }

        Value = value;
    }
}
