# 02 - Especificação Funcional: Descriptografia de Arquivos

## 1. Descrição Geral
Permite ao usuário enviar um arquivo `.krypt`, informar a senha correspondente, validar a autenticidade criptográfica (`AES-256-GCM`) de cada bloco e restaurar o arquivo original exato.

---

## 2. Atores e Rotas MVC
- **Ator:** Usuário da aplicação.
- **Rotas MVC (`DecryptionController`):**
  - `GET /Decryption/Index` — Exibe o formulário de descriptografia.
  - `POST /Decryption/Decrypt` — Descriptografa o arquivo `.krypt` e retorna o download do arquivo original restaurado.

### Fluxo Principal (Caminho Feliz)
1. O usuário acessa a tela **Descriptografar Arquivo**.
2. O usuário seleciona ou arrasta um arquivo `.krypt`.
3. O usuário digita a **Senha** utilizada na criptografia.
4. O usuário clica em **"Descriptografar e Restaurar Original"**.
5. O sistema valida o cabeçalho `KRYPT1`, deriva a chave de 256 bits via `PBKDF2-HMAC-SHA512`, autentica e descriptografa os metadados (`EncryptedMeta`), descriptografa todos os blocos de 64 KB do payload com `AES-256-GCM` e entrega o arquivo original restaurado para download.

---

## 3. Regras de Negócio (RN)
- **RN-DEC-01 (Validação de Assinatura Magic Bytes):** Se os primeiros 6 bytes do arquivo não forem `KRYPT1`, a operação é abortada imediatamente com mensagem amigável.
- **RN-DEC-02 (Verificação Rápida via Metadados Autenticados):** O sistema descriptografa primeiramente o bloco `EncryptedMeta`. Caso a senha esteja incorreta, `AuthenticationTagMismatchException` ocorre de imediato antes de processar o restante do arquivo.
- **RN-DEC-03 (Integridade Total via AES-256-GCM):** Se qualquer byte do cabeçalho ou de qualquer bloco tiver sido corrompido ou adulterado, a Tag GCM de 128 bits falhará e nenhum dado corrompido será entregue ao usuário.
- **RN-DEC-04 (Descompactação Transparente):** Se a flag de compactação estiver ativa no cabeçalho, o stream é descompactado automaticamente ao restaurar o arquivo.

---

## 4. Contrato de Dados (ViewModel)
```csharp
public sealed class DecryptFileViewModel
{
    [Required(ErrorMessage = "Selecione um arquivo .krypt para descriptografar.")]
    public IFormFile? EncryptedFile { get; set; }

    [Required(ErrorMessage = "Informe a senha para descriptografar o arquivo.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}
```

---

## 5. Critérios de Aceite (CA)
- [ ] **CA-01:** Um arquivo criptografado e descriptografado com a senha correta deve ser idêntico bit a bit ao original e preservar seu nome original.
- [ ] **CA-02:** Informar uma senha incorreta deve exibir mensagem clara (*"Senha incorreta ou o arquivo .krypt sofreu adulteração."*).
- [ ] **CA-03:** Qualquer alteração de 1 byte no arquivo `.krypt` deve ser detectada pela Tag de autenticação do `AES-256-GCM` e abortar a operação.
