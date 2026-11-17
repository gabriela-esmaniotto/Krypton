using System.Buffers.Binary;
using Krypton.Web.Models.Enums;

namespace Krypton.Web.Models.Domain;

/// <summary>
/// Representa o cabeçalho binário público de 62 bytes do formato .krypt (v1).
/// Utiliza exclusivamente AES-256-GCM + PBKDF2-HMAC-SHA512 e é autenticado via AAD em todos os blocos.
/// </summary>
public sealed class KryptHeader
{
    public const int HeaderSizeBytes = 62;
    public const int SaltSizeBytes = 32;
    public const int NonceSizeBytes = 12;
    public const int TagSizeBytes = 16;
    public const byte CurrentVersion = 0x01;
    public const int DefaultChunkSize = 65536; // 64 KB
    public const int DefaultPbkdf2Iterations = 600_000;

    private static ReadOnlySpan<byte> MagicSignature => "KRYPT1"u8;

    public byte Version { get; init; } = CurrentVersion;
    public KryptHeaderFlags Flags { get; init; } = KryptHeaderFlags.None;
    public int KdfIterations { get; init; } = DefaultPbkdf2Iterations;
    public byte[] Salt { get; init; } = new byte[SaltSizeBytes];
    public byte[] BaseNonce { get; init; } = new byte[NonceSizeBytes];
    public int ChunkSize { get; init; } = DefaultChunkSize;
    public ushort MetadataLength { get; init; }

    public bool IsCompressed => Flags.HasFlag(KryptHeaderFlags.CompressedContainer);
    public bool IsMultiFileVault => Flags.HasFlag(KryptHeaderFlags.MultiFileVault);
    public bool IsObfuscatedName => Flags.HasFlag(KryptHeaderFlags.ObfuscatedFileName);

    public byte[] ToByteArray()
    {
        var buffer = new byte[HeaderSizeBytes];
        WriteTo(buffer);
        return buffer;
    }

    public void WriteTo(Span<byte> destination)
    {
        if (destination.Length < HeaderSizeBytes)
        {
            throw new ArgumentException($"O buffer de destino precisa ter pelo menos {HeaderSizeBytes} bytes.", nameof(destination));
        }

        MagicSignature.CopyTo(destination[..6]);
        destination[6] = Version;
        destination[7] = (byte)Flags;

        BinaryPrimitives.WriteInt32LittleEndian(destination.Slice(8, 4), KdfIterations);
        Salt.AsSpan(0, SaltSizeBytes).CopyTo(destination.Slice(12, SaltSizeBytes));
        BaseNonce.AsSpan(0, NonceSizeBytes).CopyTo(destination.Slice(44, NonceSizeBytes));

        BinaryPrimitives.WriteInt32LittleEndian(destination.Slice(56, 4), ChunkSize);
        BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(60, 2), MetadataLength);
    }

    public static bool TryParse(ReadOnlySpan<byte> source, out KryptHeader? header, out string? errorMessage)
    {
        header = null;
        errorMessage = null;

        if (source.Length < HeaderSizeBytes)
        {
            errorMessage = "O arquivo é menor que o tamanho mínimo de um cabeçalho .krypt válido (62 bytes).";
            return false;
        }

        if (!source[..6].SequenceEqual(MagicSignature))
        {
            errorMessage = "Assinatura inválida: este arquivo não possui o cabeçalho 'KRYPT1' do sistema Krypton.";
            return false;
        }

        byte version = source[6];
        if (version != CurrentVersion)
        {
            errorMessage = $"Versão do formato .krypt (v{version}) não suportada.";
            return false;
        }

        var flags = (KryptHeaderFlags)source[7];
        int iterations = BinaryPrimitives.ReadInt32LittleEndian(source.Slice(8, 4));
        if (iterations <= 0 || iterations > 50_000_000)
        {
            errorMessage = "Parâmetro de iterações PBKDF2 inválido ou corrompido no cabeçalho.";
            return false;
        }

        int chunkSize = BinaryPrimitives.ReadInt32LittleEndian(source.Slice(56, 4));
        if (chunkSize < 1024 || chunkSize > 16 * 1024 * 1024)
        {
            errorMessage = "Tamanho de bloco (ChunkSize) inválido no cabeçalho.";
            return false;
        }

        ushort metaLen = BinaryPrimitives.ReadUInt16LittleEndian(source.Slice(60, 2));
        if (metaLen < TagSizeBytes + 2)
        {
            errorMessage = "Comprimento dos metadados criptografados inválido no cabeçalho.";
            return false;
        }

        header = new KryptHeader
        {
            Version = version,
            Flags = flags,
            KdfIterations = iterations,
            Salt = source.Slice(12, SaltSizeBytes).ToArray(),
            BaseNonce = source.Slice(44, NonceSizeBytes).ToArray(),
            ChunkSize = chunkSize,
            MetadataLength = metaLen
        };

        return true;
    }
}
