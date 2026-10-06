//#if Auth
using __Name__.Core.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace __Name__.DataAccess.Concrete.EntityFramework.Configurations;

public class UserOperationClaimConfiguration : IEntityTypeConfiguration<UserOperationClaim>
{
    public void Configure(EntityTypeBuilder<UserOperationClaim> builder)
    {
        builder.ToTable("UserOperationClaims");
        builder.HasKey(userOperationClaim => userOperationClaim.Id);
        builder.HasIndex(userOperationClaim => new { userOperationClaim.UserId, userOperationClaim.OperationClaimId }).IsUnique();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(userOperationClaim => userOperationClaim.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<OperationClaim>()
            .WithMany()
            .HasForeignKey(userOperationClaim => userOperationClaim.OperationClaimId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
//#endif
