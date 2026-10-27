# 10 — Organização do projeto

Em 21/10/2026, a pedido do usuário, a conversa "Transcrever instruções do aplicativo" foi vinculada ao projeto **local VanderCake**, cuja pasta é:

`C:\Users\felip\Documents\ChatGPT\VanderCake`

## Arquivos reunidos

- Código e solução diretamente na raiz do projeto, sem criar outra pasta VanderCake dentro dela.
- Planejamento e especificações em docs, incluindo AGENTS.md na raiz.
- APK, pacotes anteriores e captura da tela em outputs.
- Fotos extraídas e ZIP original do professor em references.
- Configuração local com as mesmas credenciais de demonstração.
- Banco MySQL isolado, chaves do aplicativo e chave de assinatura Android em pastas locais privadas.

O banco isolado foi encerrado antes da cópia. Os arquivos copiados foram comparados por SHA-256 antes de reiniciar a instância na nova pasta. O MySQL de serviço previamente instalado não foi alterado.

O material da origem em `C:\Users\felip\Documents\Codex\2026-10-21\tra\outputs` foi preservado como cópia de segurança. Não iniciar a API ou o banco dessa cópia simultaneamente com o projeto atual, pois utilizam as mesmas portas.

## Acesso

O endereço do painel permanece `http://localhost:5268`. As contas e senhas de desenvolvimento permanecem as mesmas; pode ser necessário entrar novamente após a transferência.

Para consultar as credenciais no seu terminal, execute a partir da raiz atual:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/Show-DevLogins.ps1
```

O script de empacotamento foi ajustado para gravar o código em outputs/VanderCake-codigo.zip, dentro do próprio projeto, excluindo banco, configurações privadas e resultados de compilação.

A organização não adiciona funcionalidades nem substitui as pendências de implementação descritas no documento 09.
