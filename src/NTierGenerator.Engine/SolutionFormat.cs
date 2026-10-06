namespace NTierGenerator.Engine;

public enum SolutionFormat
{
    /// <summary>Klasik .sln; tüm Visual Studio sürümleri ve araçlar açabilir.</summary>
    Sln,

    /// <summary>XML tabanlı yeni .slnx; Visual Studio 2022 17.13+ ve .NET 9.0.200+ SDK gerekir.</summary>
    Slnx
}
