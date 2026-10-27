# VanderCake — sistema Web/Mobile

Primeira implementação — 21/10/2026. Prazo planejado: sete dias de trabalho. Equipe: uma pessoa.

Sistema acadêmico de programação Web/Mobile para uma distribuidora de produtos de panificação.

## Pasta oficial e conversa

Esta é a raiz oficial do projeto: **C:\Users\felip\Documents\ChatGPT\VanderCake**. A conversa "Transcrever instruções do aplicativo" está vinculada ao projeto local VanderCake no aplicativo.

- `src/`: API, painel web, aplicativo e bibliotecas.
- `docs/`: planejamento, especificações e registro de validação.
- `tests/`: testes de cálculo.
- `scripts/`: execução, banco local e empacotamento.
- `references/`: fotos e arquivo original enviados pelo professor.
- `outputs/`: atalhos, pacotes ZIP e prévia visual.
- `.local/`: banco de desenvolvimento e chaves privadas locais; não compartilhar.

A organização preservou os arquivos da pasta anterior como cópia de segurança. As execuções devem partir desta nova pasta. Consulte [o registro da transferência](docs/10-organizacao-projeto.md).

## Estado atual

API ASP.NET Core, painel HTML/CSS/JavaScript e aplicativo .NET MAUI Windows implementados como primeira versão. O usuário escolheu executar o aplicativo diretamente no Windows para dispensar o emulador. MySQL possui migrations e carga fictícia; aplicativo possui SQLite para rascunhos. Consulte [o estado da implementação e suas limitações](docs/09-implementacao-validacao.md) antes de considerar a entrega acadêmica encerrada.

## Executar no Windows

Pré-requisitos validados: SDK .NET 10.0.401, workload maui-windows 10.0.20, ferramentas Windows instaladas com o Visual Studio e MySQL 8.0. Os comandos abaixo partem da pasta que contém VanderCake.sln.

### Banco isolado de desenvolvimento

```powershell
powershell -ExecutionPolicy Bypass -File scripts/Initialize-LocalMySql.ps1
dotnet run --project src/VanderCake.Api -- --migrate --seed --prepare-presentation
dotnet run --project src/VanderCake.Api --launch-profile http
```

O primeiro comando usa a instalação padrão do MySQL em C:\Program Files\MySQL\MySQL Server 8.0\bin e cria uma instância privada na porta 3307. Se necessário, informe -MySqlBin e -Port. Ele não para nem altera o serviço MySQL existente. O diretório .local contém os dados; não apague essa pasta para reiniciar o sistema.

O painel fica em **http://localhost:5268**. Não é necessário instalar Node.js para executá-lo.

Se o banco isolado já foi inicializado e o computador foi reiniciado, execute scripts/Start-LocalMySql.ps1 antes de iniciar a API. scripts/Stop-LocalMySql.ps1 encerra somente a instância privada. Não é preciso executar o seed a cada início.

### Usar seu próprio banco MySQL

Crie um schema exclusivo e um usuário com acesso a ele. Copie src/VanderCake.Api/appsettings.Example.json para appsettings.Local.json na mesma pasta, configure a conexão e substitua os placeholders das três senhas por valores próprios de pelo menos 12 caracteres. Execute --migrate --seed uma vez. Não publique esse arquivo.

### Contas de demonstração

Por preferência do usuário, as contas fictícias de desenvolvimento são **admin/admin**, **vendedor/vendedor** e **vendedor2/vendedor2**. O modo Seed:SimpleDemoPasswords só reduz o tamanho mínimo no ambiente Development. As credenciais do banco continuam separadas. Para consultar as contas no seu terminal:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/Show-DevLogins.ps1
```

O seed não altera senhas se os usuários já existem. Para aplicar as senhas demonstrativas da configuração local a essas três contas já criadas, execute `dotnet run --project src/VanderCake.Api -- --reset-demo-passwords` em Development. A operação preserva pedidos, vendas e demais registros. Não compartilhe configurações locais do banco.

### Aplicativo MAUI no Windows

```powershell
dotnet run --project src/VanderCake.Mobile
```

O comando abre uma janela nativa do Windows, sem emulador e sem instalação de pacote. No Visual Studio, selecione VanderCake.Mobile e o destino **Windows Machine**. Também é possível clicar duas vezes em **outputs/Abrir-VanderCake-Windows.cmd**.

O endereço inicial da API é **http://localhost:5268**. Entre com **vendedor/vendedor** ou **vendedor2/vendedor2**. A API e o MySQL precisam estar em execução no computador; mantenha o terminal da API aberto enquanto usa o aplicativo.

HTTP é permitido somente em Debug e para localhost/127.0.0.1. Em Release, configure HTTPS com certificado confiável. Não há código para ignorar certificados. Consulte [a execução e validação Windows](docs/11-execucao-windows.md). Confirmar com o professor se a apresentação no Windows atende ao requisito da disciplina Web/Mobile.

### Testar

Comandos usados para as verificações:

```powershell
dotnet test tests/VanderCake.Tests -m:1
powershell -ExecutionPolicy Bypass -File scripts/Smoke-Test.ps1
```

O primeiro testa rateio, parcelamento, juros e desconto compostos. O segundo usa por padrão http://127.0.0.1:5369 e exige API/banco de validação separados, com migrations e seed aplicados. Ele bloqueia a porta 5268 para preservar os dados da apresentação. Configure ConnectionStrings__MySql apenas no processo da API de validação para apontar a um schema separado e inicie essa API com --urls http://127.0.0.1:5369. Os dados criados por esse teste são descartáveis; nunca use o banco principal.

OpenAPI em desenvolvimento: http://localhost:5268/openapi/v1.json. Health check de processo: http://localhost:5268/health (não verifica o banco).

### Alterar o banco e empacotar código

```powershell
dotnet tool restore
dotnet ef migrations add NomeDaMudanca --project src/VanderCake.Api --output-dir Data/Migrations
dotnet run --project src/VanderCake.Api -- --migrate
powershell -ExecutionPolicy Bypass -File scripts/Package-Source.ps1
```

O pacote exclui bancos locais, credenciais, chaves e resultados de compilação. Mantenha backup de .local e da configuração em local privado se quiser preservar os dados da sua instalação.

## Decisões confirmadas

- Aplicativo do vendedor em .NET MAUI.
- Painel administrativo em HTML, CSS e JavaScript puro, sem Razor Pages.
- ASP.NET Core Web API compartilhada pelo aplicativo e pelo painel.
- MySQL como banco central e SQLite no aplicativo.
- Vendedor entra com conexão para baixar carteira e catálogo. Pode preparar pedidos sem conexão e usar Sincronizar pedidos para enviar a fila ao site.
- Pedidos enviados por botão.
- Falta de estoque tratada por divisão proporcional entre clientes.
- Produtos, valores, percentuais e datas das fotos são exemplos, não parâmetros obrigatórios.
- Juros e desconto compostos configuráveis por condição: padrão de demonstração de 2% por dia de atraso e 1% por dia de antecipação; taxas congeladas no pedido.

## Atualização para apresentação — 26/10/2026

Catálogo ampliado, registros exclusivos dos testes removidos e pedidos do usuário preservados. O aplicativo inclui busca de produtos, cache e fila manual de sincronização. O painel calcula uma prévia do recebimento por data e registra principal, juros e desconto separadamente. Consulte [o roteiro e as validações](docs/12-apresentacao-sincronizacao-financeiro.md).

## Como usar a documentação

| Arquivo | Conteúdo |
|---|---|
| [AGENTS.md](AGENTS.md) | Orientações para agentes e colaboradores |
| [01-escopo-requisitos.md](docs/01-escopo-requisitos.md) | Escopo, requisitos e critérios de aceite |
| [02-arquitetura-seguranca.md](docs/02-arquitetura-seguranca.md) | Componentes, organização e segurança |
| [03-regras-negocio.md](docs/03-regras-negocio.md) | Estados, estoque, corte e financeiro |
| [04-modelo-dados.md](docs/04-modelo-dados.md) | Entidades, relacionamentos e SQLite |
| [05-contrato-api.md](docs/05-contrato-api.md) | Rotas, formatos e erros |
| [06-telas-fluxos.md](docs/06-telas-fluxos.md) | Telas, navegação e comportamento |
| [07-plano-execucao-testes.md](docs/07-plano-execucao-testes.md) | Cronograma, backlog, testes e demonstração |
| [08-decisoes-riscos.md](docs/08-decisoes-riscos.md) | Premissas, pendências e riscos |
| [09-implementacao-validacao.md](docs/09-implementacao-validacao.md) | Funcionalidades implementadas, testes reais e pendências |
| [10-organizacao-projeto.md](docs/10-organizacao-projeto.md) | Pasta oficial, transferência e dados preservados |
| [11-execucao-windows.md](docs/11-execucao-windows.md) | Execução direta e validação do aplicativo Windows |
| [12-apresentacao-sincronizacao-financeiro.md](docs/12-apresentacao-sincronizacao-financeiro.md) | Catálogo, fila de sincronização, juros/desconto e roteiro |

Os caminhos relativos deste pacote são portáveis: extraia a pasta inteira para manter os vínculos.

## Resultado esperado

Um vendedor cria um pedido no celular; a administração recebe os pedidos no navegador, aplica o corte proporcional, confirma o atendimento, gera a venda e consulta ou recebe as parcelas. A aplicação mantém o pedido original e o resultado aprovado.

Os documentos 01 a 08 registram o planejamento de referência. O documento 09 distingue o que está implementado do que ainda está planejado. A primeira versão é local e não foi publicada em serviço externo.
