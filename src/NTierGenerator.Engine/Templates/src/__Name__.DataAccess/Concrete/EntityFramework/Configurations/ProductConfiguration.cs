//#if Sample
using __Name__.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace __Name__.DataAccess.Concrete.EntityFramework.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(product => product.Id);

        builder.Property(product => product.Name).IsRequired().HasMaxLength(150);
        builder.Property(product => product.UnitPrice).HasPrecision(18, 2);
        builder.HasIndex(product => product.Name).IsUnique();

        // Ürünü olan kategori silinemez; iş katmanı da bunu kontrol eder.
        builder.HasOne(product => product.Category)
            .WithMany(category => category.Products)
            .HasForeignKey(product => product.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
//#endif
