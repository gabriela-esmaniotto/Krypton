using System.Security.Cryptography;
using Krypton.Web.Models.Domain;
using Krypton.Web.Services.Implementations;
using Krypton.Web.Services.Interfaces;

namespace Krypton.Web.Infrastructure;

public static class CryptoSelfTestRunner
{
    public static async Task<int> RunAllTestsAsync()
    {
        Console.WriteLine("=== INICIANDO AUTODIAGNÓSTICO CRIPTOGRÁFICO KRYPTON (.NET 10) ===");

        var kdfService = new KeyDerivationService();
        var cryptoService = new CryptoService(kdfService);

        const string testPassword = "SenhaDeTesteSegura#2026!";

        // 1. Round-trip AES-256-GCM + PBKDF2-SHA512 (Multi-Chunk > 64 KB)
        byte[] originalData = RandomNumberGenerator.GetBytes(150_000); // ~150 KB (3 blocos de 64 KB)
        var meta = new KryptMetadata
        {
            OriginalFileName = "relatorio-confidencial.pdf",
            OriginalFileSize = originalData.Length,
            ContentType = "application/pdf"
        };

        using var inputMs1 = new MemoryStream(originalData);
        using var encryptedMs1 = new MemoryStream();
        await cryptoService.EncryptStreamAsync(inputMs1, encryptedMs1, new EncryptionOptions
        {
            Password = testPassword,
            Pbkdf2Iterations = 100_000,
            Compress = false,
            Metadata = meta
        });

        byte[] encryptedBytes1 = encryptedMs1.ToArray();
        using var encReadMs1 = new MemoryStream(encryptedBytes1);
        using var decryptedMs1 = new MemoryStream();
        DecryptionResult decResult1 = await cryptoService.DecryptStreamAsync(encReadMs1, decryptedMs1, testPassword);

        if (!decryptedMs1.ToArray().AsSpan().SequenceEqual(originalData) ||
            decResult1.Metadata.OriginalFileName != "relatorio-confidencial.pdf")
        {
            throw new InvalidOperationException("Falha no round-trip AES-256-GCM sem compactação.");
        }
        Console.WriteLine("[PASS] 1. AES-256-GCM + PBKDF2-SHA512 (Multi-Chunk 150 KB): Bytes restaurados idênticos ao original!");

        // 2. Round-trip AES-256-GCM + Compactação GZip
        using var inputMs2 = new MemoryStream(originalData);
        using var encryptedMs2 = new MemoryStream();
        await cryptoService.EncryptStreamAsync(inputMs2, encryptedMs2, new EncryptionOptions
        {
            Password = testPassword,
            Pbkdf2Iterations = 100_000,
            Compress = true,
            Metadata = meta
        });

        byte[] encryptedBytes2 = encryptedMs2.ToArray();
        using var encReadMs2 = new MemoryStream(encryptedBytes2);
        using var decryptedMs2 = new MemoryStream();
        await cryptoService.DecryptStreamAsync(encReadMs2, decryptedMs2, testPassword);

        if (!decryptedMs2.ToArray().AsSpan().SequenceEqual(originalData))
        {
            throw new InvalidOperationException("Falha no round-trip AES-256-GCM com compactação GZip.");
        }
        Console.WriteLine("[PASS] 2. AES-256-GCM + Compactação GZip: Bytes restaurados idênticos ao original!");

        // 3. Rejeição de senha incorreta
        bool wrongPasswordRejected = false;
        try
        {
            using var wrongPwdStream = new MemoryStream(encryptedBytes1);
            using var wrongPwdOut = new MemoryStream();
            await cryptoService.DecryptStreamAsync(wrongPwdStream, wrongPwdOut, "SenhaIncorreta123!");
        }
        catch (AuthenticationTagMismatchException)
        {
            wrongPasswordRejected = true;
        }
        if (!wrongPasswordRejected)
        {
            throw new InvalidOperationException("Senha incorreta não disparou AuthenticationTagMismatchException!");
        }
        Console.WriteLine("[PASS] 3. Rejeição de Senha Incorreta (AuthenticationTagMismatchException confirmada)");

        // 4. Detecção de Adulteração no Payload (Tamper Detection AES-256-GCM)
        byte[] tamperedPayload = (byte[])encryptedBytes1.Clone();
        tamperedPayload[^20] ^= 0xFF;

        bool payloadTamperCaught = false;
        try
        {
            using var tStream = new MemoryStream(tamperedPayload);
            using var tOut = new MemoryStream();
            await cryptoService.DecryptStreamAsync(tStream, tOut, testPassword);
        }
        catch (AuthenticationTagMismatchException)
        {
            payloadTamperCaught = true;
        }
        if (!payloadTamperCaught)
        {
            throw new InvalidOperationException("Adulteração no payload não foi detectada pela Tag GCM!");
        }
        Console.WriteLine("[PASS] 4. Detecção de Adulteração de Payload AES-256-GCM confirmada!");

        Console.WriteLine("=== TODOS OS 4 TESTES INTEGRADOS CONCLUÍDOS COM ÊXITO ===");
        return 0;
    }
}
