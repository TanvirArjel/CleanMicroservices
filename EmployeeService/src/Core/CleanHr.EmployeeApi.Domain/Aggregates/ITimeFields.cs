using System;

namespace CleanHr.EmployeeApi.Domain.Aggregates;

public interface ITimeFields
{
    public DateTime CreatedAtUtc { get; set; }

    public DateTime? LastModifiedAtUtc { get; set; }
}
