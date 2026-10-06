//#if Auth
using __Name__.Core.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace __Name__.DataAccess.Concrete.EntityFramework.Configurations;

public class OperationClaimConfiguration : IEntityTypeConfiguration<OperationClaim>
{
    // Seed verisi her migration'da aynı kalmalı; bu yüzden sabit tarih kullanılır.
    private static readonly DateTime SeedDate = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<OperationClaim> builder)
    {
        builder.ToTable("OperationClaims");
        builder.HasKey(operationClaim => operationClaim.Id);

        builder.Property(operationClaim => operationClaim.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(operationClaim => operationClaim.Name).IsUnique();

        // Başlangıç yetkileri. Kullanıcılara UserOperationClaims tablosu üzerinden atanır.
        builder.HasData(
//#if Sample
            new OperationClaim { Id = 1, Name = "admin", CreatedDate = SeedDate },
            new OperationClaim { Id = 2, Name = "product.add", CreatedDate = SeedDate });
//#else
            new OperationClaim { Id = 1, Name = "admin", CreatedDate = SeedDate });
//#endif
    }
}
//#endif
