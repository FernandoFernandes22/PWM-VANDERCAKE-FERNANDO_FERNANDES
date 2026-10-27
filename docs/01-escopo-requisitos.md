# 01 — Escopo e requisitos

## Objetivo

Substituir o registro disperso de pedidos por um processo conectado entre vendedor e distribuidora. A primeira versão cobre o pedido até o recebimento financeiro, sem emissão fiscal ou integração bancária.

## Perfis

**Administrador:** gerencia cadastros, carteiras, estoque, lotes de atendimento, vendas e recebimentos.

**Vendedor:** consulta sua carteira, cria clientes vinculados a si, consulta catálogo, compras e débitos de seus clientes, mantém rascunhos e envia pedidos.

## Requisitos funcionais e aceite

| ID | Requisito | Critério de aceite |
|---|---|---|
| RF01 | Autenticação e autorização | Perfis autenticados acessam somente operações permitidas; vendedor não consulta cliente de outro vendedor alterando a URL |
| RF02 | Clientes | Administrador cadastra/edita e atribui vendedor; vendedor cadastra cliente para si; dados obrigatórios são validados |
| RF03 | Localização e grupos | Estados, cidades e grupos são cadastrados/selecionados sem duplicação indevida |
| RF04 | Produtos | Administrador informa descrição, unidade, preço, grupo e situação; produto inativo não entra em novo pedido |
| RF05 | Carteira e visitas | Vendedor visualiza sua lista ordenada; administrador define a sequência de visitas |
| RF06 | Histórico e débitos | Cliente mostra compras concluídas com data, produto, quantidade e preço praticado, além do saldo a receber |
| RF07 | Rascunho local | Fechar e reabrir o app preserva pedido em elaboração no SQLite; rascunhos pertencem ao usuário logado |
| RF08 | Envio manual | Botão envia pedido válido pela API; repetição da mesma tentativa não cria novo pedido |
| RF09 | Consulta de pedidos | Vendedor acompanha seus pedidos; administrador filtra por situação, data e vendedor |
| RF10 | Estoque | Administrador consulta saldo e registra entradas/ajustes justificados; saldo não fica negativo |
| RF11 | Corte proporcional | Sistema distribui estoque entre clientes de um lote, apresenta prévia e preserva quantidades solicitadas |
| RF12 | Confirmação | Confirmação atende pedidos, baixa estoque e cria vendas/parcelas atomicamente |
| RF13 | Condições e formas | Condições contêm dias e percentuais; soma dos percentuais deve ser 100%; forma é cadastrada separadamente |
| RF14 | Contas a receber | Lista parcelas e vencimentos; registro de recebimento integral ou parcial atualiza o saldo |
| RF15 | Rastreabilidade | Pedido permite identificar vendedor, cliente, datas, valores solicitados, aprovados, venda e parcelas |

## Requisitos de qualidade

- RNF01: API acessível por HTTPS no ambiente de entrega.
- RNF02: interface MAUI utilizável no Windows, por decisão do usuário em 21/10/2026; confirmar aceite acadêmico desse destino.
- RNF03: painel funciona em navegador de desktop e mantém formulários legíveis em telas menores.
- RNF04: nenhuma operação depende de internet silenciosamente: mostrar carregamento, sucesso e erro com possibilidade de nova tentativa.
- RNF05: uso de dados fictícios na apresentação e configurações sem segredos no repositório.
- RNF06: cálculos monetários fecham em centavos e operações concorrentes não duplicam efeitos.
- RNF07: documentação final permite reproduzir a execução no ambiente validado.

## Fora do escopo inicial

Publicação em lojas, rotas em mapa, operação completa sem conexão, sincronização de pedidos em segundo plano, envio programado, emissão de boleto/nota fiscal, banco real, comissão, multiempresa, pagamentos online, lotes e validade de produtos, devoluções e cancelamento após faturamento. Em 26/10/2026 o usuário incluiu preparação de pedidos com dados locais e envio posterior por botão; esse fluxo limitado está descrito no documento 12.

## Definição de pronto

Implementação integrada em MAUI + API + painel + MySQL, uso demonstrável do SQLite, testes dos cálculos e controles críticos, roteiro de apresentação executado e instruções de configuração escritas. Uma tela isolada ou uma API sem integração não conclui a entrega.
