# 00 - Visão Geral e Arquitetura do Sistema (Krypton)

## 1. Objetivo do Sistema
O **Krypton** é uma aplicação web enxuta desenvolvida em **ASP.NET Core MVC (.NET 10)** com interface gráfica intuitiva para **criptografia e descriptografia de arquivos protegidos por senha**. O sistema utiliza um único padrão criptográfico de mercado (**AES-256-GCM** com derivação **PBKDF2-HMAC-SHA512**), processamento em *streaming* de baixo consumo de memória e política *Zero-Retention* (nenhum arquivo ou senha é armazenado de forma persistente no servidor).

---

## 2. Stack Tecnológica e Arquitetura
- **Framework Base:** .NET 10 (`net10.0`)
- **Padrão Web:** ASP.NET Core MVC (`Controllers` + `Views` Razor + `ViewModels`)
- **Algoritmo Criptográfico Único:**
  - **Cifra Autenticada (AEAD):** `AES-256-GCM` (`System.Security.Cryptography.AesGcm`, chave de 256 bits, Nonce de 96 bits, Tag de autenticação de 128 bits).
  - **Derivação de Chave (KDF):** `PBKDF2-HMAC-SHA512` (`Rfc2898DeriveBytes.Pbkdf2`, 600.000 iterações padrão OWASP, Salt de 256 bits).
- **Front-end / UI:** Razor Views (`.cshtml`), Bootstrap 5.3 (Dark/Light Mode), JavaScript moderno (Fetch API e Drag-and-Drop).

### 2.1 Estrutura de Pastas (`Krypton.Web`)
```text
Krypton.Web/
├── Controllers/
│   ├── HomeController.cs          # Painel inicial e navegação
│   ├── EncryptionController.cs    # Endpoints de criptografia (individual e lote)
│   └── DecryptionController.cs    # Endpoints de descriptografia
├── Models/
│   ├── Domain/                    # Cabeçalho .krypt (KryptHeader) e metadados (KryptMetadata)
│   ├── ViewModels/                # EncryptFileViewModel, DecryptFileViewModel, ErrorViewModel
│   └── Enums/                     # Flags de cabeçalho e modo de lote
├── Services/
│   ├── Interfaces/
│   │   ├── ICryptoService.cs
│   │   └── IKeyDerivationService.cs
│   └── Implementations/
│       ├── CryptoService.cs
│       └── KeyDerivationService.cs
└── Views/
    ├── Home/
    ├── Encryption/
    ├── Decryption/
    └── Shared/
```

---

## 3. Especificação do Formato Binário `.krypt` (v1)
Todo arquivo criptografado pelo Krypton possui a extensão `.krypt` e segue a estrutura binária simplificada abaixo:

| Offset (Bytes) | Campo | Tamanho | Descrição |
| :--- | :--- | :--- | :--- |
| `0x00` | `MagicBytes` | 6 bytes | Assinatura fixa em ASCII: `KRYPT1` (`0x4B 0x52 0x59 0x50 0x54 0x31`) |
| `0x06` | `Version` | 1 byte | Versão do formato (`0x01`) |
| `0x07` | `Flags` | 1 byte | Bitmask: `Bit 0` = Compactado (GZip/ZIP), `Bit 1` = Nome externo oculto, `Bit 2` = Cofre de múltiplos arquivos |
| `0x08` | `KdfIterations` | 4 bytes (`int32`) | Número de iterações do PBKDF2-HMAC-SHA512 (padrão: `600000`) |
| `0x0C` | `Salt` | 32 bytes | Salt criptograficamente aleatório gerado por `RandomNumberGenerator` |
| `0x2C` | `BaseNonce` | 12 bytes | Nonce base de 96 bits para AES-256-GCM (incrementado por bloco via XOR com o índice do chunk) |
| `0x38` | `ChunkSize` | 4 bytes (`int32`) | Tamanho do bloco de payload em bytes (padrão: `65536` = 64 KB) |
| `0x3C` | `MetadataLength`| 2 bytes (`uint16`)| Tamanho do bloco de metadados criptografados |
| `0x3E` | `EncryptedMeta` | `N` bytes | JSON criptografado (nome original, tamanho original, timestamp UTC, MIME type) + `16 bytes` Tag GCM |
| `0x3E + N` | `PayloadChunks` | Variável | Sequência de blocos: cada bloco contém `[Ciphertext (até ChunkSize)] + [Tag GCM (16 bytes)]` |

> **Autenticidade de Cabeçalho (AAD):** Os 62 bytes do cabeçalho (`0x00` a `0x3D`) são passados como **AAD (*Associated Authenticated Data*)** no AES-256-GCM dos metadados e de cada chunk.

---

## 4. Requisitos Não-Funcionais
1. **Zero-Persistence:** Senhas e chaves derivadas nunca são gravadas em disco ou logs; buffers de chave são limpos com `CryptographicOperations.ZeroMemory()`.
2. **Processamento em Blocos (64 KB):** Suporte a arquivos grandes com baixo uso de memória.
3. **Proteção CSRF:** Endpoints `POST` validam `[ValidateAntiForgeryToken]`.
