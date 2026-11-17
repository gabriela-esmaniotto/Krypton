using System.Security.Cryptography;
using Krypton.Web.Models.ViewModels;
using Krypton.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Krypton.Web.Controllers;

public sealed class DecryptionController : Controller
{
    private readonly ICryptoService _cryptoService;

    public DecryptionController(ICryptoService cryptoService)
    {
        _cryptoService = cryptoService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View(new DecryptFileViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(524_288_000)]
    public async Task<IActionResult> Decrypt([FromForm] DecryptFileViewModel model, CancellationToken cancellationToken)
    {
        if (model.EncryptedFile is null || model.EncryptedFile.Length == 0)
        {
            return BadRequest(new { error = "Selecione um arquivo .krypt válido para descriptografar." });
        }

        if (string.IsNullOrEmpty(model.Password))
        {
            return BadRequest(new { error = "Informe a senha para descriptografar o arquivo." });
        }

        try
        {
            await using Stream inputStream = model.EncryptedFile.OpenReadStream();
            using var decryptedStream = new MemoryStream();

            DecryptionResult result = await _cryptoService.DecryptStreamAsync(
                inputStream,
                decryptedStream,
                model.Password,
                cancellationToken);

            byte[] restoredBytes = decryptedStream.ToArray();

            string restoredFileName = Path.GetFileName(result.Metadata.OriginalFileName);
            if (string.IsNullOrWhiteSpace(restoredFileName))
            {
                restoredFileName = "arquivo-restaurado.bin";
            }

            string contentType = string.IsNullOrWhiteSpace(result.Metadata.ContentType)
                ? "application/octet-stream"
                : result.Metadata.ContentType;

            Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
            Response.Headers["X-Krypton-OutputFileName"] = Uri.EscapeDataString(restoredFileName);

            return File(restoredBytes, contentType, restoredFileName);
        }
        catch (AuthenticationTagMismatchException)
        {
            return BadRequest(new
            {
                error = "Falha de Autenticação (AES-256-GCM): Senha incorreta ou o arquivo .krypt sofreu adulteração."
            });
        }
        catch (CryptographicException ex)
        {
            return BadRequest(new
            {
                error = $"Erro criptográfico ao validar o arquivo: {ex.Message}"
            });
        }
        catch (InvalidDataException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = $"Erro ao descriptografar arquivo: {ex.Message}" });
        }
    }
}
