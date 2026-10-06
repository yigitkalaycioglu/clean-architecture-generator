namespace NTierGenerator.Engine.Templating;

/// <summary>Şablon sözdizimi hatası (kapanmamış #if, bilinmeyen sembol vb.).</summary>
public sealed class TemplateException(string message) : Exception(message);
