# 08 — Decisões, premissas e riscos

## Confirmado pelo usuário

| Decisão | Consequência |
|---|---|
| Projeto Web/Mobile, uma pessoa e uma semana | Priorizar processo completo e interface simples |
| .NET MAUI, API, MySQL e SQLite mobile | Dois clientes e persistência central/local obrigatórios |
| Administração em HTML/CSS/JS padrão | Sem Razor Pages; frontend estático com fetch |
| Carteira/catálogo carregados com conexão; pedidos podem ser preparados sem conexão | Fila SQLite e botão de sincronização, conforme atualização de 26/10/2026 |
| Envio por botão | Sem agendador às 12h/19h |
| Corte proporcional entre clientes | Agrupar por cliente antes de distribuir por pedido |
| Fotos contêm exemplos | Não fixar valores ou datas no código |
| Execução direta do MAUI Windows com dotnet run | Sem emulador Android; decisão de 21/10/2026 |

## Propostas adotadas para tornar o projeto implementável

Estas decisões não foram explicitamente confirmadas pelo usuário e podem ser ajustadas antes ou durante o primeiro dia:

- Painel servido em wwwroot da API, mesma origem.
- EF Core e migrations, com provedor MySQL compatível.
- Destino de desenvolvimento e demonstração: MAUI Windows, conforme solicitação posterior do usuário.
- Unidades comerciais inteiras; sem peso/fracionamento.
- Vendas e parcelas criadas ao confirmar o atendimento, sem módulo de entrega.
- Corte por rodada selecionada pelo administrador, com maiores restos e desempate documentado.
- Condição/forma escolhidas no pedido e congeladas no envio.
- Estoque simples, sem lote/validade, embora os produtos sejam perecíveis.
- Recebimentos parciais permitidos; sem estorno/cancelamento após venda.
- Juros e desconto compostos por dia, confirmados em 26/10/2026 e descritos nos documentos 03 e 12; sem multa fixa adicional.
- Roteiro é sequência fixa da carteira, sem mapa nem agenda diária.
- Sem emissão fiscal; “venda” é registro interno acadêmico.

## Perguntas que ainda podem alterar o escopo

1. Confirmar com o professor o enquadramento final das regras de encargos confirmadas pelo usuário.
2. O professor aceita a demonstração do MAUI no Windows para a disciplina Web/Mobile?
3. Há necessidade de venda por peso/fracionada ou controle de validade/lote?
4. Qual é a data exata da entrega e quantas horas por dia estarão disponíveis?

Essas perguntas não impedem criar a base. A primeira muda o financeiro; a segunda muda o preparo da entrega; a terceira muda dados e rateio; a quarta muda a capacidade do cronograma.

## Riscos e resposta

| Risco | Impacto | Ação |
|---|---|---|
| Destino Windows não aceito pelo professor | Pode exigir adaptação da entrega | Confirmar o formato de apresentação acadêmica |
| Versões incompatíveis de .NET/EF/provedor | Bloqueia persistência/build | Inspecionar e fixar combinação testada |
| Escopo grande para uma pessoa | Entrega incompleta | Caminho principal primeiro, UI mínima e dia final reservado |
| Rateio incorreto e duplicação por repetição | Estoque/vendas inconsistentes | Algoritmo exato, transação, constraints e idempotência |
| Prévia deixa de representar saldo atual | Atendimento inválido | Revalidar e exigir nova prévia |
| SQLite usado só como decoração | Requisito acadêmico mal atendido | Persistir e demonstrar rascunho sobrevivendo ao reinício |
| Crescimento para modo offline completo | Sincronização inviável no prazo | Manter operações oficiais online |
| Professor exige encargos automáticos | Financeiro incompleto | Esclarecer cedo e substituir tarefas de menor prioridade |

## Gestão de mudanças

Mudança confirmada deve atualizar requisito, regra, modelo, contrato, tela e teste afetados. Registrar brevemente data, decisão e impacto neste arquivo. Não tratar novas funcionalidades como gratuitas dentro da semana.

## Histórico

- 26/10/2026: usuário confirmou juros de 2% e desconto de 1% compostos por dia, configuráveis. Autorizou fila de pedidos sem conexão e envio posterior por botão. Implementação, limites e verificações no documento 12.
- 26/10/2026: usuário relatou texto ilegível no aplicativo com o Windows em modo escuro; adotado tema claro consistente no aplicativo. Sobre encargos, indicou acreditar em juros compostos; ainda pendentes confirmação da regra, desconto, taxas configuráveis e tratamento de pagamentos parciais. Cobrança automática continua sem implementação.
- 21/10/2026: pacote inicial de planejamento criado com base nas fotos e nas respostas do usuário. Nenhuma implementação foi iniciada neste pacote.
- 21/10/2026: usuário autorizou o início da implementação. Primeira fatia funcional e validações registradas no documento 09; diferenças de escopo permanecem explícitas.
