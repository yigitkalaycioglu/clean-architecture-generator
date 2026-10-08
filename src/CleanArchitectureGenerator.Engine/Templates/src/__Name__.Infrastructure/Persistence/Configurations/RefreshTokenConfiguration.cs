//#if LocalAuth
using __Name__.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace __Name__.Infrastructure.Persistence.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(token => token.Id);
        builder.Property(token => token.Id).ValueGeneratedNever();

        // SHA-256 özetinin Base64 hali 44 karakterdir.
        builder.Property(token => token.TokenHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(token => token.TokenHash).IsUnique();
        builder.HasIndex(token => token.FamilyId);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
//#endif
