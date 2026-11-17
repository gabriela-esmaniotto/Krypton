namespace Krypton.Web.Models.Domain;

/// <summary>
/// Metadados do arquivo original, armazenados em JSON UTF-8 e criptografados via AES-256-GCM no bloco EncryptedMeta.
/// </summary>
public sealed class KryptMetadata
{
    public string OriginalFileName { get; set; } = string.Empty;
    public long OriginalFileSize { get; set; }
    public string ContentType { get; set; } = "application/octet-stream";
    public DateTime EncryptedAtUtc { get; set; } = DateTime.UtcNow;
    public int FileCount { get; set; } = 1;
}
