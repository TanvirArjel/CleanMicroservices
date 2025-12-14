using System.Diagnostics.CodeAnalysis;

namespace CleanHr.DepartmentApi;

/// <summary>
/// Marker class used by WebApplicationFactory for integration testing.
/// This class serves as an entry point reference for the test host.
/// </summary>
[SuppressMessage("Microsoft.Design", "CA1515:Consider making public types internal", Justification = "This class must be public for WebApplicationFactory to access it from integration tests.")]
public sealed class DepartmentApiMarker
{
}
