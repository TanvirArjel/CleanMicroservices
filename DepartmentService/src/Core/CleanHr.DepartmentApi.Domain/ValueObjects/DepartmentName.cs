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
        Value = value;
    }
}
