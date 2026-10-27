# 09 — Primeira implementação e validação

Atualizado em 26/10/2026. Este arquivo registra fatos da implementação; consulte também a atualização de apresentação no documento 12.

## Entregue nesta etapa

- Solução com API, biblioteca de regras, contratos compartilhados, MAUI Windows e testes.
- API com autenticação por cookie para o painel e token opaco protegido para MAUI; CSRF no painel, hash de senha e separação de carteiras.
- Clientes: inclusão, consulta, edição administrativa, vendedor responsável e ordem de visita.
- Produtos: inclusão, consulta, edição e inativação, preço e unidade inteira.
- Estoque: saldo e movimentos de ajuste/entrada, com proteção de concorrência.
- Condições: inclusão, consulta, edição/inativação e taxas de juros/desconto; percentuais e prazos validados. Formas: inclusão e consulta.
- Pedidos: criação, consulta, idempotência, validação de preço e cancelamento administrativo antes do atendimento.
- Corte proporcional agrupado por cliente, maiores restos, prévia protegida e confirmação transacional.
- Venda e parcelas geradas na confirmação; recebimento parcial/integral, saldo e histórico.
- Painel administrativo funcional em HTML/CSS/JavaScript, hospedado pela API, sem Razor.
- MAUI: login, carteira, novo cliente, rascunhos, catálogo, envio, meus pedidos, histórico e débitos.
- SQLite: rascunhos e fila manual com payload JSON e chave persistente de tentativa; cache de carteira, produtos, condições e formas por usuário. Sessão em SecureStorage.
- Migrations reais e seed fictício; scripts para banco local, testes, credenciais de demonstração e pacote sem segredos.

## Tecnologias efetivamente utilizadas

| Componente | Versão |
|---|---|
| SDK .NET | 10.0.401 |
| ASP.NET Core instalado | 10.0.12 |
| Microsoft.Maui.Controls | 10.0.20 |
| MAUI Windows workload | 10.0.20 |
| MySQL local isolado | 8.0.46, porta 3307 |
| MySql.EntityFrameworkCore | 10.0.9 |
| EF Core Design | 10.0.9 |
| OpenAPI ASP.NET Core | 10.0.12 |
| sqlite-net-pcl | 1.11.285 |
| xUnit | 2.9.3 |

Referências de compatibilidade dos pacotes: [conector MySQL](https://www.nuget.org/packages/MySql.EntityFrameworkCore/10.0.9) e [SQLite](https://www.nuget.org/packages/sqlite-net-pcl/1.11.285). A escolha foi feita a partir dos componentes instalados e confirmada por compilação e testes locais.

## Verificação executada

- API: compilação com sucesso, sem avisos/erros na verificação final.
- MAUI Windows: compilação Debug com zero avisos/erros e abertura da janela nativa por dotnet run.
- Testes unitários: **20 aprovados**, nenhum ignorado. Incluem rateio, parcelamento e seis novos testes de juros/desconto compostos, amortização e limites.
- Integração HTTP com MySQL real: **23 verificações aprovadas** pelo script Smoke-Test.ps1.
- Testado: login mobile, isolamento entre carteiras, idempotência de pedido/atendimento/recebimento, distribuição 3/6/6, parcelas fechando o total, pagamento parcial, histórico e CSRF.
- Testado: prévia invalidada por mudança de estoque, atendimentos concorrentes, recebimentos concorrentes, corte total e ausência de vantagem ao fracionar pedidos.
- Navegador: tela de login renderizada e rejeição de credenciais inválidas conferidas na interface.

O teste HTTP simula os clientes e comprova o fluxo do servidor. O usuário optou pela execução do MAUI no Windows; emulador, imagens, SDK local e APK foram removidos. O usuário confirmou login e carregamento da carteira na janela Windows usando localhost. Envio e persistência após reinício ainda devem ser confirmados na interface. Consulte o documento 11.

## Ajustes de implementação em relação ao modelo inicial

- Primeira fatia usa cidade, UF e grupo como campos textuais. Cadastros normalizados de Estado/Cidade/Grupo e respectivos seletores ainda precisam ser implementados.
- Estoque está na entidade Produto, com Version de concorrência e tabela de movimentos. Não há tabela Estoque separada.
- SQLite persiste um payload JSON por rascunho em vez de linhas normalizadas de itens. Isso mantém conteúdo e chave da tentativa juntos em uma única gravação.
- Condições ficam congeladas em JSON no pedido, junto aos nomes da condição/forma. Venda referencia o pedido imutável em vez de duplicar ItemVenda.
- Lote persiste vendas vinculadas; pedidos totalmente cortados guardam o resultado em si, mas ainda não há vínculo explícito lote/pedido para consultar essas exclusões pelo lote.
- Token mobile é opaco via ASP.NET Data Protection, sem JWT/refresh token. Expira em uma hora.
- Juros/desconto compostos implementados com prévia, taxas congeladas e histórico separado de principal/ajustes. Regra e limites detalhados no documento 03. Não há multa fixa adicional.

Essas diferenças são explícitas e não encerram os requisitos acadêmicos do planejamento.

## Próximas tarefas para concluir a primeira versão acadêmica

1. Executar o MAUI Windows e verificar persistência após reinício, recuperação de timeout e troca de conta.
2. Revisar visualmente o painel autenticado e seus formulários.
3. Normalizar cidades/estados/grupos e implementar seus cadastros, alinhando contratos e telas.
4. Completar edição/inativação de formas, cadastro/edição de vendedores e filtros planejados; condições já possuem edição/inativação.
5. Completar auditoria de cancelamento (data/autor) e vínculo explícito do lote com todos os pedidos.
6. Confirmar produtos fracionados e formato final de apresentação. Encargos já seguem a regra confirmada pelo usuário em 26/10/2026.

## Limites do ambiente local

A instância MySQL e as credenciais de desenvolvimento ficam em .local e appsettings.Local.json, excluídos do pacote. O serviço MySQL previamente existente permaneceu inalterado. Migração e execução com TLS precisaram ocorrer fora do ambiente restrito de ferramentas, usando a configuração normal do Windows.

As chaves Data Protection ficam em pasta .local da API no ambiente de desenvolvimento. Antes de publicação, definir proteção de chaves em repouso, acesso restrito, HTTPS e configuração de produção. O aplicativo Windows é executado localmente em Debug, sem pacote de loja.

Durante a restauração houve indisponibilidade da consulta de vulnerabilidades do NuGet; isso não foi tratado como verificação de segurança aprovada. A restauração usou pacotes já disponíveis localmente. As compilações finais não apresentaram erros.
