using System.ComponentModel.DataAnnotations;

namespace Krypton.Web.Models.Enums;

/// <summary>
/// Bitmask de flags armazenado no cabeçalho .krypt.
/// </summary>
[Flags]
public enum KryptHeaderFlags : byte
{
    None = 0,
    CompressedContainer = 1 << 0,  // Bit 0: Conteúdo compactado (GZip ou Container ZIP de lote)
    ObfuscatedFileName = 1 << 1,   // Bit 1: Nome externo ofuscado
    MultiFileVault = 1 << 2        // Bit 2: Cofre contendo múltiplos arquivos empacotados em ZIP
}

/// <summary>
/// Modo de operação quando múltiplos arquivos são enviados simultaneamente.
/// </summary>
public enum BatchEncryptionMode
{
    [Display(Name = "Cofre Único (.krypt) — Empacota todos os arquivos em um único container criptografado")]
    SingleEncryptedVault = 1,

    [Display(Name = "Arquivos Individuais (.zip) — Criptografa cada arquivo separadamente e entrega em um .zip")]
    IndividualKryptFiles = 2
}
