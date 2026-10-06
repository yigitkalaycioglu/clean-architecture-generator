using System.Text.Encodings.Web;
using System.Text.Unicode;
using __Name__.Business;
using __Name__.Business.DependencyResolvers.Autofac;
using __Name__.Core.DependencyResolvers;
using __Name__.Core.Extensions;
using __Name__.Core.Utilities.IoC;
using __Name__.DataAccess;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.WebEncoders;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------- Servisler
// Autofac: Business katmanındaki manager'ları aspect (AOP) desteğiyle kaydeder.
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(container => container.RegisterModule(new AutofacBusinessModule()));

builder.Services.AddBusinessServices(builder.Configuration);
builder.Services.AddDependencyResolvers(new CoreModule());
builder.Services.AddControllersWithViews();

// Türkçe karakterler HTML çıktısında &#xFC; gibi kodlanmadan, olduğu gibi yazılır.
builder.Services.Configure<WebEncoderOptions>(options =>
    options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));

var app = builder.Build();

// Aspect'ler attribute olduğu için servislere ServiceTool üzerinden erişir.
ServiceTool.Initialize(app.Services);

// ---------------------------------------------------------------- HTTP istek hattı
if (app.Environment.IsDevelopment())
{
    // Bekleyen migration'lar varsa veritabanını günceller (yalnızca geliştirme ortamında).
    await app.Services.ApplyDatabaseMigrationsAsync();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
