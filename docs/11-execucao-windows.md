# 11 — Execução direta no Windows

Atualizado em 21/10/2026. Por solicitação do usuário, o aplicativo .NET MAUI passa a rodar diretamente no Windows, sem emulador. O nome do projeto VanderCake.Mobile foi mantido para preservar os vínculos da solução.

## Abrir

A partir da raiz do projeto, com o MySQL local e a API ativos:

```powershell
dotnet run --project src/VanderCake.Mobile
```

Também pode abrir outputs/Abrir-VanderCake-Windows.cmd ou selecionar VanderCake.Mobile / Windows Machine no Visual Studio. O aplicativo usa net10.0-windows10.0.19041.0, sem empacotamento MSIX. A carga maui-windows 10.0.20 já estava instalada; não foi necessária instalação de ferramentas.

API: http://localhost:5268. Login do aplicativo: vendedor/vendedor; painel: admin/admin. O aplicativo continua separado do painel web: ambos usam a API, que persiste no MySQL; rascunhos e cache do aplicativo usam SQLite local. Credenciais de sessão ficam no SecureStorage.

## Alterações e limpeza

- Emulador VanderCake_API35 e compilação Android encerrados.
- Removidos do projeto: SDK Android local, imagem do sistema, dados do dispositivo virtual, downloads, APK, chave Debug, scripts de preparação/abertura e fontes específicas de Android.
- Instalações compartilhadas do Visual Studio e SDKs anteriores ao projeto preservadas.
- MySQL, pedidos e arquivos de configuração local preservados.
- Destino Windows configurado no projeto e endereço inicial da API alterado para localhost.

## Validação

- Compilação MAUI Windows aprovada: zero avisos e zero erros.
- dotnet run abriu processo e janela nativa do aplicativo.
- Árvore de acessibilidade confirmou formulário de login e endereço http://localhost:5268.
- Usuário confirmou login vendedor/vendedor e carregamento da lista de clientes na interface Windows.
- Captura automática da janela falhou com FrameArrived timed out; validação da interface acompanhada com o usuário.
- As três contas demonstrativas foram autenticadas pela API, e senha incorreta recebeu 401.
- Persistência do rascunho após fechar/reabrir, envio e consulta no painel ainda precisam ser demonstrados na interface Windows.

O destino é uma decisão de execução do usuário. Confirmar com o professor se a demonstração no Windows atende à disciplina Web/Mobile; não afirmar que uma versão Windows é um APK Android.

## Legibilidade com o Windows em modo escuro — 26/10/2026

O aplicativo combina telas e cartões claros. Os controles herdavam o tema escuro do sistema, produzindo textos brancos sobre essas superfícies. O tema claro foi definido no MAUI e na inicialização nativa do WinUI, sem alterar as preferências do Windows. Textos de ajuda e valores desabilitados receberam cinza mais escuro para melhorar o contraste. A validação visual depende de conferir os campos de login, quantidades, seletores de pagamento e valores na janela atualizada.
