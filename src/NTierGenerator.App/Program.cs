using NTierGenerator.Engine;
using NTierGenerator.Engine.Templating;

namespace NTierGenerator.App;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) =>
            MessageBox.Show(e.Exception.Message, "Beklenmeyen hata", MessageBoxButtons.OK, MessageBoxIcon.Error);

        var generator = new SolutionGenerator(CreateTemplateSource());
        Application.Run(new MainForm(generator, AppSettings.Load()));
    }

    /// <summary>
    /// Uygulamanın yanında "Templates" klasörü varsa şablonlar oradan okunur; böylece şablonlar
    /// yeniden derleme yapmadan özelleştirilebilir. Yoksa exe'ye gömülü şablonlar kullanılır.
    /// </summary>
    private static ITemplateSource CreateTemplateSource()
    {
        var customDirectory = Path.Combine(AppContext.BaseDirectory, "Templates");
        return Directory.Exists(customDirectory) ? new DirectoryTemplateSource(customDirectory) : EmbeddedTemplateSource.Default;
    }
}
