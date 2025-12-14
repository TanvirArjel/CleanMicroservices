namespace CleanHr.DepartmentApi.IntegrationTests;

public static class TestConstants
{
    public const string ApiVersion = "1.0";
    public static readonly string BaseUrl = $"/api/v{ApiVersion}/departments";

    public static class TestDepartments
    {
        public static readonly Guid ItDepartmentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly Guid HrDepartmentId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        public static readonly Guid FinanceDepartmentId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        public static readonly Guid NonExistentId = Guid.Parse("99999999-9999-9999-9999-999999999999");

        public const string ItDepartmentName = "IT Department";
        public const string HrDepartmentName = "HR Department";
        public const string FinanceDepartmentName = "Finance Department";
    }
}
