namespace CleanHr.DepartmentApi.IntegrationTests.Models;

public class CreateDepartmentRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
}

public class UpdateDepartmentRequest
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
}

public class DepartmentResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? LastModifiedAtUtc { get; set; }
}

public class SelectListItem
{
    public bool Disabled { get; set; }
    public object? Group { get; set; }
    public bool Selected { get; set; }
    public string? Text { get; set; }
    public string? Value { get; set; }
}
