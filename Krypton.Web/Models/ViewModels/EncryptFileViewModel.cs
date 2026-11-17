using System.ComponentModel.DataAnnotations;
using Krypton.Web.Models.Enums;

namespace Krypton.Web.Models.ViewModels;

public sealed class EncryptFileViewModel
{
    [Required(ErrorMessage = "Selecione ao menos um arquivo para criptografar.")]
    public List<IFormFile> Files { get; set; } = new();

    [Required(ErrorMessage = "Informe a senha de criptografia.")]
    [MinLength(8, ErrorMessage = "A senha deve ter pelo menos 8 caracteres.")]
    [DataType(DataType.Password)]
    public string? Password { get; set; }

    [Required(ErrorMessage = "Confirme a senha de criptografia.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "A confirmação de senha não confere com a senha informada.")]
    public string? ConfirmPassword { get; set; }

    public bool CompressBeforeEncrypt { get; set; } = false;

    public bool ObfuscateOutputFileName { get; set; } = false;

    public BatchEncryptionMode BatchMode { get; set; } = BatchEncryptionMode.SingleEncryptedVault;
}
