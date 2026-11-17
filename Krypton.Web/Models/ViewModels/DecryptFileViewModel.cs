using System.ComponentModel.DataAnnotations;

namespace Krypton.Web.Models.ViewModels;

public sealed class DecryptFileViewModel
{
    [Required(ErrorMessage = "Selecione um arquivo .krypt para descriptografar.")]
    public IFormFile? EncryptedFile { get; set; }

    [Required(ErrorMessage = "Informe a senha do arquivo.")]
    [DataType(DataType.Password)]
    public string? Password { get; set; }
}
