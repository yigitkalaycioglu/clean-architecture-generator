//#if Auth
using __Name__.Core.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace __Name__.DataAccess.Concrete.EntityFramework.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(user => user.Id);

        builder.Property(user => user.FirstName).IsRequired().HasMaxLength(50);
        builder.Property(user => user.LastName).IsRequired().HasMaxLength(50);
        builder.Property(user => user.Email).IsRequired().HasMaxLength(256);
        builder.Property(user => user.PasswordHash).IsRequired();
        builder.Property(user => user.PasswordSalt).IsRequired();
        builder.HasIndex(user => user.Email).IsUnique();
    }
}
//#endif
