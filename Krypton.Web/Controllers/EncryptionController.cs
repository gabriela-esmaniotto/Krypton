using System.IO.Compression;
using System.Security.Cryptography;
using Krypton.Web.Models.Domain;
using Krypton.Web.Models.Enums;
using Krypton.Web.Models.ViewModels;
using Krypton.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Krypton.Web.Controllers;

public sealed class EncryptionController : Controller
{
    private readonly ICryptoService _cryptoService;

    public EncryptionController(ICryptoService cryptoService)
    {
        _cryptoService = cryptoService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View(new EncryptFileViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(524_288_000)]
    public async Task<IActionResult> Encrypt([FromForm] EncryptFileViewModel model, CancellationToken cancellationToken)
    {
        if (model.Files is null || model.Files.Count == 0 || model.Files.All(f => f.Length == 0))
        {
            return BadRequestError("Selecione pelo menos um arquivo válido para criptografar.");
        }

        if (model.Files.Count > 50)
        {
            return BadRequestError("O limite máximo por lote é de 50 arquivos.");
        }

        if (string.IsNullOrEmpty(model.Password) || model.Password.Length < 8)
        {
            return BadRequestError("A senha de criptografia é obrigatória e deve ter pelo menos 8 caracteres.");
        }

        if (!string.Equals(model.Password, model.ConfirmPassword, StringComparison.Ordinal))
        {
            return BadRequestError("A confirmação de senha não confere com a senha informada.");
        }

        try
        {
            // Caso 1: Arquivo Único
            if (model.Files.Count == 1)
            {
                IFormFile singleFile = model.Files[0];
                string safeOriginalName = Path.GetFileName(singleFile.FileName);

                await using Stream inputStream = singleFile.OpenReadStream();

                var options = new EncryptionOptions
                {
                    Password = model.Password,
                    Compress = model.CompressBeforeEncrypt,
                    IsMultiFileVault = false,
                    ObfuscateFileName = model.ObfuscateOutputFileName,
                    Metadata = new KryptMetadata
                    {
                        OriginalFileName = safeOriginalName,
                        OriginalFileSize = singleFile.Length,
                        ContentType = string.IsNullOrWhiteSpace(singleFile.ContentType)
                            ? "application/octet-stream"
                            : singleFile.ContentType,
                        EncryptedAtUtc = DateTime.UtcNow,
                        FileCount = 1
                    }
                };

                using var outputStream = new MemoryStream();
                await _cryptoService.EncryptStreamAsync(inputStream, outputStream, options, cancellationToken);

                byte[] outputBytes = outputStream.ToArray();
                string downloadFileName = model.ObfuscateOutputFileName
                    ? $"vault-{Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(4))}.krypt"
                    : $"{safeOriginalName}.krypt";

                SetKryptonResponseHeaders(downloadFileName);
                return File(outputBytes, "application/octet-stream", downloadFileName);
            }

            // Caso 2: Lote em Cofre Único (.krypt contendo pacote ZIP criptografado)
            if (model.BatchMode == BatchEncryptionMode.SingleEncryptedVault)
            {
                using var zipMemoryStream = new MemoryStream();

                using (var archive = new ZipArchive(zipMemoryStream, ZipArchiveMode.Create, leaveOpen: true))
                {
                    var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (IFormFile formFile in model.Files)
                    {
                        string entryName = GetUniqueSafeFileName(Path.GetFileName(formFile.FileName), usedNames);
                        ZipArchiveEntry entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
                        await using Stream entryStream = entry.Open();
                        await using Stream fileStream = formFile.OpenReadStream();
                        await fileStream.CopyToAsync(entryStream, cancellationToken);
                    }
                }

                zipMemoryStream.Position = 0;
                string vaultOriginalZipName = $"Krypton-Vault-{DateTime.UtcNow:yyyyMMdd-HHmmss}.zip";

                var vaultOptions = new EncryptionOptions
                {
                    Password = model.Password,
                    Compress = true,
                    IsMultiFileVault = true,
                    ObfuscateFileName = model.ObfuscateOutputFileName,
                    Metadata = new KryptMetadata
                    {
                        OriginalFileName = vaultOriginalZipName,
                        OriginalFileSize = zipMemoryStream.Length,
                        ContentType = "application/zip",
                        EncryptedAtUtc = DateTime.UtcNow,
                        FileCount = model.Files.Count
                    }
                };

                using var encryptedVaultStream = new MemoryStream();
                await _cryptoService.EncryptStreamAsync(zipMemoryStream, encryptedVaultStream, vaultOptions, cancellationToken);

                byte[] vaultBytes = encryptedVaultStream.ToArray();
                string downloadVaultName = model.ObfuscateOutputFileName
                    ? $"vault-{Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(4))}.krypt"
                    : $"Krypton-Vault-{model.Files.Count}files.krypt";

                SetKryptonResponseHeaders(downloadVaultName);
                return File(vaultBytes, "application/octet-stream", downloadVaultName);
            }

            // Caso 3: Lote com Criptografia Individual (Pacote .zip com N arquivos .krypt independentes)
            {
                using var batchZipStream = new MemoryStream();
                using (var archive = new ZipArchive(batchZipStream, ZipArchiveMode.Create, leaveOpen: true))
                {
                    var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (IFormFile formFile in model.Files)
                    {
                        string safeName = Path.GetFileName(formFile.FileName);
                        await using Stream fileStream = formFile.OpenReadStream();

                        var itemOptions = new EncryptionOptions
                        {
                            Password = model.Password,
                            Compress = model.CompressBeforeEncrypt,
                            IsMultiFileVault = false,
                            ObfuscateFileName = model.ObfuscateOutputFileName,
                            Metadata = new KryptMetadata
                            {
                                OriginalFileName = safeName,
                                OriginalFileSize = formFile.Length,
                                ContentType = string.IsNullOrWhiteSpace(formFile.ContentType)
                                    ? "application/octet-stream"
                                    : formFile.ContentType,
                                EncryptedAtUtc = DateTime.UtcNow,
                                FileCount = 1
                            }
                        };

                        string kryptEntryBase = model.ObfuscateOutputFileName
                            ? $"vault-{Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(4))}.krypt"
                            : $"{safeName}.krypt";

                        string uniqueEntryName = GetUniqueSafeFileName(kryptEntryBase, usedNames);
                        ZipArchiveEntry entry = archive.CreateEntry(uniqueEntryName, CompressionLevel.NoCompression);
                        await using Stream entryStream = entry.Open();
                        await _cryptoService.EncryptStreamAsync(fileStream, entryStream, itemOptions, cancellationToken);
                    }
                }

                byte[] batchBytes = batchZipStream.ToArray();
                string batchFileName = $"Krypton-Batch-{model.Files.Count}files.zip";

                SetKryptonResponseHeaders(batchFileName);
                return File(batchBytes, "application/zip", batchFileName);
            }
        }
        catch (Exception ex)
        {
            return BadRequestError($"Falha ao criptografar o arquivo: {ex.Message}");
        }
    }

    private void SetKryptonResponseHeaders(string fileName)
    {
        Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
        Response.Headers["X-Krypton-OutputFileName"] = Uri.EscapeDataString(fileName);
    }

    private static string GetUniqueSafeFileName(string fileName, HashSet<string> usedNames)
    {
        string sanitized = string.IsNullOrWhiteSpace(fileName) ? "file.bin" : fileName;
        if (usedNames.Add(sanitized))
        {
            return sanitized;
        }

        string nameWithoutExt = Path.GetFileNameWithoutExtension(sanitized);
        string ext = Path.GetExtension(sanitized);
        int counter = 2;
        while (true)
        {
            string candidate = $"{nameWithoutExt} ({counter}){ext}";
            if (usedNames.Add(candidate))
            {
                return candidate;
            }
            counter++;
        }
    }

    private IActionResult BadRequestError(string message)
    {
        return BadRequest(new { error = message });
    }
}
