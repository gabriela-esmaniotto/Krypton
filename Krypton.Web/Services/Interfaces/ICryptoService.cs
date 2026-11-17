using Krypton.Web.Models.Domain;

namespace Krypton.Web.Services.Interfaces;

public sealed class EncryptionOptions
{
    public required string Password { get; init; }
    public int Pbkdf2Iterations { get; init; } = KryptHeader.DefaultPbkdf2Iterations;
    public int ChunkSize { get; init; } = KryptHeader.DefaultChunkSize;
    public bool Compress { get; init; }
    public bool IsMultiFileVault { get; init; }
    public bool ObfuscateFileName { get; init; }
    public KryptMetadata Metadata { get; init; } = new();
}

public sealed class DecryptionResult
{
    public required KryptHeader Header { get; init; }
    public required KryptMetadata Metadata { get; init; }
}

public interface ICryptoService
{
    /// <summary>
    /// Criptografa um stream de entrada para o formato .krypt v1 usando AES-256-GCM em blocos de 64 KB.
    /// </summary>
    Task EncryptStreamAsync(
        Stream inputStream,
        Stream outputStream,
        EncryptionOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Descriptografa e valida a autenticidade AES-256-GCM de um stream .krypt v1.
    /// </summary>
    Task<DecryptionResult> DecryptStreamAsync(
        Stream encryptedInputStream,
        Stream decryptedOutputStream,
        string password,
        CancellationToken cancellationToken = default);
}
