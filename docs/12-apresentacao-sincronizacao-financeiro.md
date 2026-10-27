# 12 — Apresentação, sincronização e recebimentos

Atualização de 26/10/2026, solicitada pelo usuário.

## Dados de apresentação

Backup privado anterior às alterações em .local/backups. Foram removidos 19 pedidos compostos exclusivamente por produtos dos testes automatizados, com seus efeitos financeiros relacionados. Os dois pedidos feitos pelo usuário foram preservados. Produtos de teste sem referências foram excluídos; os oito produtos usados nos pedidos preservados receberam nomes/códigos comerciais fictícios distintos, sem mudar os valores desses pedidos. O catálogo final tem 28 produtos ativos: farinhas, açúcares, chocolates, recheios, coberturas, misturas, ingredientes e embalagens. Não há dados pessoais reais nessa carga.

O comando Development --prepare-presentation prepara o catálogo e remove somente registros identificados como testes. Não executá-lo rotineiramente no início do sistema. As contas permanecem admin/admin, vendedor/vendedor e vendedor2/vendedor2.

## Pedido sem conexão

1. Entre com internet e aguarde carregar a carteira; nessa etapa o aplicativo baixa clientes, produtos, condições e formas de pagamento.
2. Sem conexão, abra um cliente já carregado, monte um pedido e confirme Enviar pedido / guardar na fila. Os dados locais são identificados como catálogo salvo e o pedido fica em Aguardando sincronização.
3. Com conexão, na carteira clique em Sincronizar pedidos. O botão envia somente pedidos confirmados para a fila ou tentativas com envio incerto; rascunhos em elaboração não são enviados.
4. Confira Meus pedidos e a lista de pedidos do site. O envio não confirma o atendimento, não baixa estoque e não significa entrega física.

Cada tentativa conserva uma chave UUID no SQLite. Falha de conexão mantém o pedido; a repetição usa a mesma chave. Mudança de preço ou produto indisponível devolve o pedido à edição para conferência. A API continua verificando carteira, preços e condições. Se a sessão de uma hora expirar, entre novamente com conexão; a fila pertence à conta e permanece no SQLite. Cadastro de novos clientes, histórico e recebimento continuam exigindo conexão.

## Recebimentos

Em Pagamentos, edite ou crie uma condição com as taxas desejadas. O padrão dos novos pedidos usa 2% ao dia de atraso e 1% ao dia antecipado, compostos e em dias corridos. Taxas anteriores dos pedidos existentes são preservadas.

Em Contas a receber, Receber abre a simulação para a data escolhida: principal, juros, desconto e total para quitar. Pode informar recebimento parcial; a baixa de principal e dos ajustes é proporcional. Histórico mostra o dinheiro recebido e os ajustes efetivos. Não há integração bancária.

Exemplos para principal de R$100: três dias antecipados = R$97,03; vencimento = R$100; três dias atrasados = R$106,12. Fórmulas e arredondamento no documento 03.

## Verificações executadas

- 20 testes unitários aprovados, incluindo seis novos testes de juros/desconto compostos, amortização, limites e quitação.
- 23 verificações HTTP/MySQL de pedidos, isolamento, idempotência, rateio e concorrência aprovadas em banco temporário separado.
- 12 verificações financeiras HTTP/MySQL aprovadas no mesmo ambiente isolado: taxas congeladas, antecipação, atraso, parcial, histórico, quitação, saldo antigo, excesso e autorização. Datas históricas criadas somente nas fixtures desse banco.
- Migração aplicada ao banco principal, preservando as baixas anteriores.
- Compilações da API e do MAUI Windows aprovadas sem avisos/erros.
- Navegador: taxas 2%/1% e formulário de edição conferidos na interface. Usuário confirmou o botão Sincronizar pedidos na carteira do aplicativo.
- Banco e API temporários encerrados após os testes. O painel principal permanece sem os registros desses testes.

A captura automática do aplicativo Windows falhou nas etapas anteriores; a nova interface e o fluxo real de criação sem conexão, reinício e sincronização ainda precisam de validação visual acompanhada. Não confundir testes HTTP de repetição segura com validação completa da interface offline.

## Roteiro antes da apresentação

Demonstrar: entrar no app, carregar carteira, montar pedido sem conexão, fechar/reabrir e conferir a fila, reconectar e sincronizar, conferir um único pedido no site, preparar/confirmar atendimento e abrir a simulação do recebimento. Para demonstrar desconto, escolher uma condição com vencimento futuro. A confirmação de atendimento continua sendo responsabilidade da administração.
