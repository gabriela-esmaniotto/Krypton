namespace Krypton.Web.Services.Interfaces;

public interface IKeyDerivationService
{
    /// <summary>
    /// Deriva uma chave de 256 bits (32 bytes) a partir da senha e do Salt usando PBKDF2-HMAC-SHA512.
    /// </summary>
    byte[] DeriveKey(string password, ReadOnlySpan<byte> salt, int iterations);
}
