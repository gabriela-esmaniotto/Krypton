# 03 - Especificação Funcional: Processamento em Lote e Compactação

## 1. Descrição Geral
Permite ao usuário selecionar múltiplos arquivos simultaneamente na tela de criptografia, oferecendo dois modos de saída:
1. **Modo Cofre Único (Container `.krypt`):** Empacota todos os arquivos selecionados em um arquivo ZIP em memória/stream e criptografa o pacote inteiro em um único arquivo `.krypt` com `AES-256-GCM`.
2. **Modo Arquivos Individuais (`.zip` de `.krypt`):** Criptografa cada arquivo separadamente (cada um com seu próprio `Salt` e `BaseNonce`, usando a mesma senha) e entrega todos agrupados em um `.zip`.

---

## 2. Regras de Negócio (RN)
- **RN-BAT-01 (Modo Cofre Único - Padrão para Múltiplos Arquivos):**
  - Quando o usuário seleciona 2 ou mais arquivos e mantém a opção **"Cofre Único (`.krypt`)"**, o sistema cria um `ZipArchive` contendo todos os arquivos originais e criptografa o stream resultante em um único arquivo `.krypt`.
- **RN-BAT-02 (Modo Criptografia Individual em Lote):**
  - Quando o usuário escolhe **"Arquivos Individuais (`.zip`)"**, cada arquivo recebe um `Salt` e `BaseNonce` exclusivos, gerando `arquivo1.ext.krypt`, `arquivo2.ext.krypt`, etc., entregues dentro de um arquivo `.zip`.
- **RN-BAT-03 (Limites de Lote):**
  - Quantidade máxima de arquivos por lote: `50 arquivos`.
  - Tamanho máximo total do lote: `500 MB`.
- **RN-BAT-04 (Sanitização de Nomes):**
  - Todos os nomes de entradas em pacotes `.zip` são sanitizados com `Path.GetFileName()` para impedir ataques de *Path Traversal / Zip Slip*.

---

## 3. Critérios de Aceite (CA)
- [ ] **CA-01:** Ao selecionar múltiplos arquivos, a interface lista todos os arquivos com tamanho individual, tamanho total e botão para remover itens da fila.
- [ ] **CA-02:** O seletor de modo de lote (*"Cofre Único `.krypt`"* vs. *"Arquivos `.krypt` individuais"*) é exibido automaticamente quando há 2 ou mais arquivos selecionados.
- [ ] **CA-03:** Ao descriptografar um `.krypt` gerado no modo "Cofre Único", o usuário recebe o pacote `.zip` restaurado contendo todos os arquivos originais.
