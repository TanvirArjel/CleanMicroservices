using CleanHr.EmployeeApi.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanHr.EmployeeApi.Persistence.RelationalDB.EntityConfigurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");
        builder.HasKey(emp => emp.Id);

        builder.Property(emp => emp.FirstName)
            .HasMaxLength(50).IsRequired();

        builder.Property(emp => emp.LastName)
            .HasMaxLength(50).IsRequired();

        // DepartmentId is a foreign key reference to DepartmentService (different microservice)
        builder.Property(emp => emp.DepartmentId)
            .IsRequired();

        builder.Property(emp => emp.DateOfBirth)
            .HasColumnType("date").IsRequired();

        builder.Property(emp => emp.Email)
            .HasMaxLength(50).IsRequired();

        builder.HasIndex(emp => emp.Email).IsUnique();

        builder.Property(emp => emp.PhoneNumber)
            .HasMaxLength(15).IsRequired();

        builder.HasIndex(emp => emp.PhoneNumber).IsUnique();
    }
}
