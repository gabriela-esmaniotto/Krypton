using System.Security.Cryptography;
using System.Text;
using Krypton.Web.Services.Interfaces;

namespace Krypton.Web.Services.Implementations;

public sealed class KeyDerivationService : IKeyDerivationService
{
    private const int DerivedKeySizeBytes = 32; // 256 bits para AES-256-GCM

    public byte[] DeriveKey(string password, ReadOnlySpan<byte> salt, int iterations)
    {
        if (string.IsNullOrEmpty(password))
        {
            throw new ArgumentException("A senha é obrigatória para derivar a chave criptográfica.", nameof(password));
        }

        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
        try
        {
            int effectiveIterations = Math.Max(iterations, 10_000);
            return Rfc2898DeriveBytes.Pbkdf2(
                passwordBytes,
                salt,
                effectiveIterations,
                HashAlgorithmName.SHA512,
                DerivedKeySizeBytes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(passwordBytes);
        }
    }
}
