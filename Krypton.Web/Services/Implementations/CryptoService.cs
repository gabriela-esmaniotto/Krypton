using System.Buffers;
using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Krypton.Web.Models.Domain;
using Krypton.Web.Models.Enums;
using Krypton.Web.Services.Interfaces;

namespace Krypton.Web.Services.Implementations;

public sealed class CryptoService : ICryptoService
{
    private const ulong MetadataNonceMask = 0xFFFFFFFFFFFFFFFFUL;
    private readonly IKeyDerivationService _keyDerivationService;

    public CryptoService(IKeyDerivationService keyDerivationService)
    {
        _keyDerivationService = keyDerivationService;
    }

    public async Task EncryptStreamAsync(
        Stream inputStream,
        Stream outputStream,
        EncryptionOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(inputStream);
        ArgumentNullException.ThrowIfNull(outputStream);
        ArgumentNullException.ThrowIfNull(options);

        byte[] salt = RandomNumberGenerator.GetBytes(KryptHeader.SaltSizeBytes);
        byte[] baseNonce = RandomNumberGenerator.GetBytes(KryptHeader.NonceSizeBytes);

        var flags = KryptHeaderFlags.None;
        if (options.Compress)
        {
            flags |= KryptHeaderFlags.CompressedContainer;
        }
        if (options.IsMultiFileVault)
        {
            flags |= KryptHeaderFlags.MultiFileVault;
        }
        if (options.ObfuscateFileName)
        {
            flags |= KryptHeaderFlags.ObfuscatedFileName;
        }

        byte[] metaPlainBytes = JsonSerializer.SerializeToUtf8Bytes(options.Metadata);
        ushort encryptedMetaLength = checked((ushort)(metaPlainBytes.Length + KryptHeader.TagSizeBytes));

        var header = new KryptHeader
        {
            Version = KryptHeader.CurrentVersion,
            Flags = flags,
            KdfIterations = options.Pbkdf2Iterations,
            Salt = salt,
            BaseNonce = baseNonce,
            ChunkSize = options.ChunkSize,
            MetadataLength = encryptedMetaLength
        };

        byte[] headerBytes = header.ToByteArray();
        byte[] derivedKey = _keyDerivationService.DeriveKey(options.Password, salt, options.Pbkdf2Iterations);

        try
        {
            using var aesGcm = new AesGcm(derivedKey, KryptHeader.TagSizeBytes);

            // 1. Escreve o cabeçalho público de 62 bytes (protegido contra adulteração via AAD)
            await outputStream.WriteAsync(headerBytes, cancellationToken);

            // 2. Criptografa e escreve o bloco de metadados (EncryptedMeta)
            byte[] encryptedMeta = new byte[encryptedMetaLength];
            byte[] metaNonce = new byte[KryptHeader.NonceSizeBytes];
            DeriveBlockNonce(baseNonce, MetadataNonceMask, metaNonce);

            aesGcm.Encrypt(
                metaNonce,
                metaPlainBytes,
                encryptedMeta.AsSpan(0, metaPlainBytes.Length),
                encryptedMeta.AsSpan(metaPlainBytes.Length, KryptHeader.TagSizeBytes),
                headerBytes);

            await outputStream.WriteAsync(encryptedMeta, cancellationToken);

            // 3. Compacta opcionalmente com GZip antes de criptografar os blocos
            Stream sourceToEncrypt = inputStream;
            MemoryStream? compressedStream = null;

            try
            {
                if (options.Compress && !options.IsMultiFileVault)
                {
                    compressedStream = new MemoryStream();
                    await using (var gzip = new GZipStream(compressedStream, CompressionLevel.Optimal, leaveOpen: true))
                    {
                        await inputStream.CopyToAsync(gzip, cancellationToken);
                    }
                    compressedStream.Position = 0;
                    sourceToEncrypt = compressedStream;
                }

                await EncryptPayloadChunksAsync(
                    aesGcm,
                    sourceToEncrypt,
                    outputStream,
                    baseNonce,
                    headerBytes,
                    options.ChunkSize,
                    cancellationToken);
            }
            finally
            {
                if (compressedStream is not null)
                {
                    await compressedStream.DisposeAsync();
                }
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(derivedKey);
        }
    }

    public async Task<DecryptionResult> DecryptStreamAsync(
        Stream encryptedInputStream,
        Stream decryptedOutputStream,
        string password,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(encryptedInputStream);
        ArgumentNullException.ThrowIfNull(decryptedOutputStream);

        if (string.IsNullOrEmpty(password))
        {
            throw new InvalidOperationException("Informe a senha para descriptografar o arquivo.");
        }

        byte[] headerBytes = new byte[KryptHeader.HeaderSizeBytes];
        int headerRead = await ReadExactAsync(encryptedInputStream, headerBytes, cancellationToken);
        if (headerRead < KryptHeader.HeaderSizeBytes)
        {
            throw new InvalidDataException("O arquivo enviado é menor que o cabeçalho .krypt mínimo (62 bytes).");
        }

        if (!KryptHeader.TryParse(headerBytes, out KryptHeader? header, out string? error) || header is null)
        {
            throw new InvalidDataException(error ?? "O arquivo enviado não possui um cabeçalho .krypt válido.");
        }

        byte[] derivedKey = _keyDerivationService.DeriveKey(password, header.Salt, header.KdfIterations);

        try
        {
            using var aesGcm = new AesGcm(derivedKey, KryptHeader.TagSizeBytes);

            // 1. Valida a senha imediatamente autenticando e descriptografando os metadados
            KryptMetadata metadata = await ReadAndDecryptMetadataAsync(
                aesGcm,
                encryptedInputStream,
                header,
                headerBytes,
                cancellationToken);

            // 2. Descriptografa todos os blocos do payload
            if (header.IsCompressed && !header.IsMultiFileVault)
            {
                using var tempCompressed = new MemoryStream();
                await DecryptPayloadChunksAsync(
                    aesGcm,
                    encryptedInputStream,
                    tempCompressed,
                    header,
                    headerBytes,
                    cancellationToken);

                tempCompressed.Position = 0;
                await using var gzip = new GZipStream(tempCompressed, CompressionMode.Decompress, leaveOpen: true);
                await gzip.CopyToAsync(decryptedOutputStream, cancellationToken);
            }
            else
            {
                await DecryptPayloadChunksAsync(
                    aesGcm,
                    encryptedInputStream,
                    decryptedOutputStream,
                    header,
                    headerBytes,
                    cancellationToken);
            }

            return new DecryptionResult
            {
                Header = header,
                Metadata = metadata
            };
        }
        finally
        {
            CryptographicOperations.ZeroMemory(derivedKey);
        }
    }

    private static async Task<KryptMetadata> ReadAndDecryptMetadataAsync(
        AesGcm aesGcm,
        Stream encryptedInputStream,
        KryptHeader header,
        byte[] headerBytes,
        CancellationToken cancellationToken)
    {
        byte[] encryptedMeta = new byte[header.MetadataLength];
        int readMeta = await ReadExactAsync(encryptedInputStream, encryptedMeta, cancellationToken);
        if (readMeta < header.MetadataLength)
        {
            throw new InvalidDataException("Arquivo .krypt truncado no bloco de metadados.");
        }

        int cipherLen = header.MetadataLength - KryptHeader.TagSizeBytes;
        byte[] plainMetaBytes = new byte[cipherLen];
        byte[] metaNonce = new byte[KryptHeader.NonceSizeBytes];
        DeriveBlockNonce(header.BaseNonce, MetadataNonceMask, metaNonce);

        aesGcm.Decrypt(
            metaNonce,
            encryptedMeta.AsSpan(0, cipherLen),
            encryptedMeta.AsSpan(cipherLen, KryptHeader.TagSizeBytes),
            plainMetaBytes,
            headerBytes);

        return JsonSerializer.Deserialize<KryptMetadata>(plainMetaBytes)
               ?? throw new InvalidDataException("Não foi possível desserializar os metadados do arquivo.");
    }

    private static async Task EncryptPayloadChunksAsync(
        AesGcm aesGcm,
        Stream inputStream,
        Stream outputStream,
        byte[] baseNonce,
        byte[] headerBytes,
        int chunkSize,
        CancellationToken cancellationToken)
    {
        byte[] currentBuffer = ArrayPool<byte>.Shared.Rent(chunkSize);
        byte[] nextBuffer = ArrayPool<byte>.Shared.Rent(chunkSize);
        byte[] encryptedChunkBuffer = ArrayPool<byte>.Shared.Rent(chunkSize + KryptHeader.TagSizeBytes);
        byte[] chunkNonce = new byte[KryptHeader.NonceSizeBytes];
        byte[] chunkAad = new byte[KryptHeader.HeaderSizeBytes + 9];
        headerBytes.CopyTo(chunkAad, 0);

        try
        {
            int currentCount = await ReadExactAsync(inputStream, currentBuffer.AsMemory(0, chunkSize), cancellationToken);
            ulong chunkIndex = 0;

            while (true)
            {
                int nextCount = await ReadExactAsync(inputStream, nextBuffer.AsMemory(0, chunkSize), cancellationToken);
                bool isFinalChunk = nextCount == 0;

                BinaryPrimitives.WriteUInt64LittleEndian(chunkAad.AsSpan(KryptHeader.HeaderSizeBytes, 8), chunkIndex);
                chunkAad[KryptHeader.HeaderSizeBytes + 8] = isFinalChunk ? (byte)1 : (byte)0;

                DeriveBlockNonce(baseNonce, chunkIndex, chunkNonce);

                aesGcm.Encrypt(
                    chunkNonce,
                    currentBuffer.AsSpan(0, currentCount),
                    encryptedChunkBuffer.AsSpan(0, currentCount),
                    encryptedChunkBuffer.AsSpan(currentCount, KryptHeader.TagSizeBytes),
                    chunkAad);

                await outputStream.WriteAsync(
                    encryptedChunkBuffer.AsMemory(0, currentCount + KryptHeader.TagSizeBytes),
                    cancellationToken);

                if (isFinalChunk)
                {
                    break;
                }

                (currentBuffer, nextBuffer) = (nextBuffer, currentBuffer);
                currentCount = nextCount;
                chunkIndex++;
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(currentBuffer);
            CryptographicOperations.ZeroMemory(nextBuffer);
            ArrayPool<byte>.Shared.Return(currentBuffer);
            ArrayPool<byte>.Shared.Return(nextBuffer);
            ArrayPool<byte>.Shared.Return(encryptedChunkBuffer);
        }
    }

    private static async Task DecryptPayloadChunksAsync(
        AesGcm aesGcm,
        Stream encryptedInputStream,
        Stream decryptedOutputStream,
        KryptHeader header,
        byte[] headerBytes,
        CancellationToken cancellationToken)
    {
        int maxEncryptedChunkSize = header.ChunkSize + KryptHeader.TagSizeBytes;
        byte[] currentBuffer = ArrayPool<byte>.Shared.Rent(maxEncryptedChunkSize);
        byte[] nextBuffer = ArrayPool<byte>.Shared.Rent(maxEncryptedChunkSize);
        byte[] plainBuffer = ArrayPool<byte>.Shared.Rent(header.ChunkSize);
        byte[] chunkNonce = new byte[KryptHeader.NonceSizeBytes];
        byte[] chunkAad = new byte[KryptHeader.HeaderSizeBytes + 9];
        headerBytes.CopyTo(chunkAad, 0);

        try
        {
            int currentCount = await ReadExactAsync(encryptedInputStream, currentBuffer.AsMemory(0, maxEncryptedChunkSize), cancellationToken);
            if (currentCount < KryptHeader.TagSizeBytes)
            {
                throw new CryptographicException("Arquivo .krypt truncado ou corrompido: bloco de dados ausente.");
            }

            ulong chunkIndex = 0;

            while (true)
            {
                int nextCount = await ReadExactAsync(encryptedInputStream, nextBuffer.AsMemory(0, maxEncryptedChunkSize), cancellationToken);
                bool isFinalChunk = nextCount == 0;

                if (currentCount < KryptHeader.TagSizeBytes)
                {
                    throw new CryptographicException($"Bloco #{chunkIndex} corrompido: tamanho menor que a Tag GCM.");
                }

                int cipherBytesLen = currentCount - KryptHeader.TagSizeBytes;

                BinaryPrimitives.WriteUInt64LittleEndian(chunkAad.AsSpan(KryptHeader.HeaderSizeBytes, 8), chunkIndex);
                chunkAad[KryptHeader.HeaderSizeBytes + 8] = isFinalChunk ? (byte)1 : (byte)0;

                DeriveBlockNonce(header.BaseNonce, chunkIndex, chunkNonce);

                aesGcm.Decrypt(
                    chunkNonce,
                    currentBuffer.AsSpan(0, cipherBytesLen),
                    currentBuffer.AsSpan(cipherBytesLen, KryptHeader.TagSizeBytes),
                    plainBuffer.AsSpan(0, cipherBytesLen),
                    chunkAad);

                if (cipherBytesLen > 0)
                {
                    await decryptedOutputStream.WriteAsync(plainBuffer.AsMemory(0, cipherBytesLen), cancellationToken);
                }

                if (isFinalChunk)
                {
                    break;
                }

                (currentBuffer, nextBuffer) = (nextBuffer, currentBuffer);
                currentCount = nextCount;
                chunkIndex++;
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plainBuffer);
            ArrayPool<byte>.Shared.Return(currentBuffer);
            ArrayPool<byte>.Shared.Return(nextBuffer);
            ArrayPool<byte>.Shared.Return(plainBuffer);
        }
    }

    private static void DeriveBlockNonce(ReadOnlySpan<byte> baseNonce, ulong blockCounter, Span<byte> destinationNonce)
    {
        baseNonce.CopyTo(destinationNonce);
        ulong low64 = BinaryPrimitives.ReadUInt64LittleEndian(destinationNonce.Slice(4, 8));
        BinaryPrimitives.WriteUInt64LittleEndian(destinationNonce.Slice(4, 8), low64 ^ blockCounter);
    }

    private static async Task<int> ReadExactAsync(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken)
    {
        int totalRead = 0;
        while (totalRead < buffer.Length)
        {
            int read = await stream.ReadAsync(buffer[totalRead..], cancellationToken);
            if (read == 0)
            {
                break;
            }
            totalRead += read;
        }
        return totalRead;
    }
}
