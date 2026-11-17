# Especificações do Sistema — Krypton (.NET 10 ASP.NET Core MVC)

Este diretório contém as especificações funcionais, técnicas e de interface gráfica (UI/UX) do **Krypton**, uma ferramenta web direta e enxuta para criptografia e descriptografia autenticada de arquivos desenvolvida em **ASP.NET Core MVC (.NET 10)**.

## Índice de Especificações

| # | Documento | Escopo Principal |
| :--- | :--- | :--- |
| **00** | [00-visao-geral-e-arquitetura.md](./00-visao-geral-e-arquitetura.md) | Arquitetura ASP.NET Core MVC (.NET 10), algoritmo único `AES-256-GCM`, derivação `PBKDF2-HMAC-SHA512` e formato binário `.krypt` v1. |
| **01** | [01-criptografia-de-arquivos.md](./01-criptografia-de-arquivos.md) | Fluxo de criptografia de arquivos protegidos por senha, opções de compactação/ofuscação de nome e regras de negócio. |
| **02** | [02-descriptografia-de-arquivos.md](./02-descriptografia-de-arquivos.md) | Fluxo de descriptografia com validação de autenticidade por bloco (AEAD Tag) e restauração do arquivo original. |
| **03** | [03-processamento-em-lote-e-compactacao.md](./03-processamento-em-lote-e-compactacao.md) | Criptografia de múltiplos arquivos em lote (Modo Cofre Único `.krypt` ou Arquivos `.krypt` individuais empacotados em `.zip`). |
| **04** | [04-interface-grafica-e-experiencia-usuario.md](./04-interface-grafica-e-experiencia-usuario.md) | Mapa de navegação enxuto (`Painel`, `Criptografar`, `Descriptografar`), componentes de UI (*Drag-and-Drop*, *Dark/Light Mode*) e usabilidade. |
| **05** | [05-diagramas-uml-e-modelagem.md](./05-diagramas-uml-e-modelagem.md) | Modelagem de software: a) Modelo Conceitual, b) Diagrama de Casos de Uso, c) Casos de Uso Textuais, d) Diagrama de Classes e e) Diagramas de Sequência. |
