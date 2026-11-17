# 01 - Especificação Funcional: Criptografia de Arquivos

## 1. Descrição Geral
Permite ao usuário selecionar um ou mais arquivos através da interface gráfica web, definir uma senha de proteção e realizar o download do arquivo criptografado em **AES-256-GCM** no formato `.krypt`.

---

## 2. Atores e Rotas MVC
- **Ator:** Usuário da aplicação.
- **Rotas MVC (`EncryptionController`):**
  - `GET /Encryption/Index` — Exibe o formulário de criptografia.
  - `POST /Encryption/Encrypt` — Processa o upload, criptografa com `AES-256-GCM` e retorna o arquivo `.krypt`.

### Fluxo Principal (Caminho Feliz)
1. O usuário acessa a tela **Criptografar Arquivos**.
2. O usuário arrasta ou seleciona o(s) arquivo(s) desejado(s) na zona de *Drag-and-Drop*.
3. O usuário informa a **Senha de Criptografia** (mínimo de 8 caracteres) e a **Confirmação de Senha**.
4. *(Opcional)* O usuário marca as opções adicionais:
   - **Compactar antes de criptografar (GZip):** Reduz o tamanho antes de aplicar a cifra.
   - **Ocultar nome externo do arquivo:** Gera um nome aleatório (ex.: `vault-a1b2c3d4.krypt`), mantendo o nome verdadeiro protegido dentro dos metadados criptografados.
5. O usuário clica em **"Criptografar e Baixar (.krypt)"**.
6. O sistema deriva a chave de 256 bits via `PBKDF2-HMAC-SHA512`, criptografa os metadados e o conteúdo em blocos de 64 KB usando `AES-256-GCM` e inicia o download do arquivo `.krypt`.

---

## 3. Regras de Negócio (RN)
- **RN-ENC-01 (Validação de Senha):** O usuário deve fornecer uma senha com no mínimo 8 caracteres e confirmação idêntica.
- **RN-ENC-02 (Algoritmo Único Padronizado):** Todas as operações utilizam exclusivamente `AES-256-GCM` com chave de 256 bits derivada por `PBKDF2-HMAC-SHA512` (600.000 iterações).
- **RN-ENC-03 (Unicidade de Salt e Nonce):** Cada arquivo criptografado gera um novo `Salt` de 32 bytes e um novo `BaseNonce` de 12 bytes via `RandomNumberGenerator.GetBytes()`.
- **RN-ENC-04 (Prevenção de Truncamento em Streaming):** Cada bloco de 64 KB inclui seu índice sequencial e um indicador de bloco final (`isFinalChunk`) no AAD do `AES-256-GCM`.
- **RN-ENC-05 (Limpeza de Memória):** A chave derivada de 32 bytes é zerada da RAM com `CryptographicOperations.ZeroMemory()` no bloco `finally`.

---

## 4. Contrato de Dados (ViewModel)
```csharp
public sealed class EncryptFileViewModel
{
    [Required(ErrorMessage = "Selecione ao menos um arquivo para criptografar.")]
    public List<IFormFile> Files { get; set; } = new();

    [Required(ErrorMessage = "Informe a senha de criptografia.")]
    [MinLength(8, ErrorMessage = "A senha deve ter pelo menos 8 caracteres.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirme a senha de criptografia.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "As senhas informadas não conferem.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public bool CompressBeforeEncrypt { get; set; } = false;

    public bool ObfuscateOutputFileName { get; set; } = false;

    public BatchEncryptionMode BatchMode { get; set; } = BatchEncryptionMode.SingleEncryptedVault;
}
```

---

## 5. Critérios de Aceite (CA)
- [ ] **CA-01:** Criptografar qualquer arquivo deve gerar um arquivo `.krypt` autenticado com cabeçalho `KRYPT1`.
- [ ] **CA-02:** Criptografar o mesmo arquivo duas vezes com a mesma senha deve produzir saídas binárias diferentes devido ao `Salt` e `BaseNonce` aleatórios.
- [ ] **CA-03:** Se a confirmação de senha divergir da senha principal, o sistema deve impedir a operação e exibir mensagem clara.
