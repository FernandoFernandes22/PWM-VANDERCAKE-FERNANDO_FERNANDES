# 07 — Execução, testes e entrega

## Cronograma relativo

Sete dias contados do início acordado, sem presumir data final. A quantidade de horas disponíveis ainda não foi informada.

| Dia | Atividades | Marco verificável |
|---|---|---|
| 1 | Validar SDK/workloads, MySQL e conexão MAUI; criar solução, migrations e autenticação; testar navegador/app chamando API | Login e uma consulta no MAUI Windows e navegador |
| 2 | Clientes, localização, grupos, produtos, condições, formas, estoque e seeds fictícios; formulários simples | Administrador cadastra dados e vendedor consulta carteira |
| 3 | MAUI com catálogo, rascunhos SQLite e envio idempotente; listagem administrativa | Pedido criado no celular aparece no site |
| 4 | Prévia proporcional, confirmação transacional e controle concorrente; testes de corte | Rodada é atendida sem estoque negativo |
| 5 | Vendas, parcelas, recebimentos e consultas de histórico/débitos | Pedido chega até o pagamento |
| 6 | Integração, permissões, erros, preço alterado, timeout e acabamento essencial | Cenários críticos passam e fluxo pode ser demonstrado |
| 7 | Reserva para correções, dados de apresentação, documentação de execução e ensaio | Entrega reproduzível e limitações registradas |

O cronograma é uma meta apertada. Se o marco do dia 1 falhar, resolver ambiente antes de ampliar telas. Se o fluxo do dia 3 atrasar, reduzir decoração e cadastros auxiliares a telas mínimas/seeds documentados; não retirar API, MAUI, SQLite, MySQL ou corte proporcional. Requisitos do professor prevalecem sobre cortes de escopo.

## Backlog ordenado

1. P0 — ambiente alcançável e autenticação/autorização dos dois clientes.
2. P0 — dados mínimos e leitura de carteira/catálogo.
3. P0 — persistência real de rascunho SQLite e envio à API.
4. P0 — pedidos no painel e corte proporcional por cliente.
5. P0 — confirmação atômica com estoque, venda e parcelas.
6. P0 — recebimento e saldo do cliente.
7. P1 — manutenção completa de referências, filtros e mensagens.
8. P1 — histórico e sequência de visitas com apresentação refinada.
9. P2 — melhorias visuais adicionais, somente após integração.

P1 continua no escopo, mas é implementado com interface simples. Uma eventual retirada precisa ser registrada, não escondida.

## Verificações significativas

| ID | Cenário | Resultado esperado |
|---|---|---|
| T01 | Vendedor tenta acessar cliente/pedido alheio | Sem acesso aos dados ou escrita |
| T02 | Reiniciar app após montar pedido | Rascunho e chave preservados no SQLite |
| T03 | Repetir envio por timeout/clique duplo | Um único pedido; conteúdo diferente com mesma chave é rejeitado |
| T04 | Preço mudou entre consulta e envio | Conflito claro e confirmação de novo preço necessária |
| T05 | Estoque 15; demandas 5, 8, 9 | Aprovados 3, 6, 6 |
| T06 | Um cliente divide sua demanda em vários pedidos | Rateio do cliente não aumenta por fracionamento |
| T07 | Estoque zero, suficiente e empate de restos | Zero, atendimento completo e desempate determinístico |
| T08 | Estoque muda após prévia | 409; nenhuma venda/baixa/parcela criada |
| T09 | Dois administradores confirmam pedidos sobrepostos | Sem dupla venda e sem estoque negativo |
| T10 | Repetir confirmação ou simular falha intermediária | Mesmo resultado ou rollback integral |
| T11 | Venda 235 em duas metades; 100 em três percentuais | Parcelas fecham 235,00 e 100,00 |
| T12 | Parcela de valor muito pequeno arredonda para zero | Operação rejeitada com mensagem útil |
| T13 | Receber parcialmente e depois quitar | Saldo e estado corretos; saldo nunca negativo |
| T14 | Dois recebimentos concorrentes excederiam o saldo | Revalidação impede excedente |
| T15 | Pedido com todos os itens cortados | NAO_ATENDIDO e nenhuma venda/parcela |
| T16 | Produto/cliente inativo e quantidade inválida | Rejeição pelo servidor |
| T17 | Trocar conta no MAUI | Nenhum rascunho/cache do outro usuário visível |
| T18 | Escrita no painel sem CSRF e sessão expirada | Rejeição; reautenticação segura |

Testes unitários para rateio e parcelamento; integração com MySQL para transações, constraints, idempotência e concorrência. Não substituir esses testes por banco em memória que não reproduz o comportamento do MySQL. Teste manual no MAUI Windows para SQLite, teclado, reinício e perda de conexão.

## Dados fictícios de demonstração

Administrador, dois vendedores para provar isolamento, três clientes na mesma rodada e um fora dela. Produto com saldo 15 e pedidos 5/8/9; outro produto sem saldo e outro com saldo suficiente. Condições à vista, duas parcelas de 50% e três de 33,33/33,33/33,34. Senhas de seed fornecidas por configuração de desenvolvimento; não gravar credenciais reais nos documentos.

## Checklist de entrega

- [ ] Solução compila no ambiente documentado.
- [ ] API, MySQL, painel e MAUI comunicam-se de verdade.
- [ ] Migrations e seed reproduzíveis, com configuração de exemplo.
- [ ] SQLite demonstrado por reinício do aplicativo.
- [ ] OpenAPI corresponde aos endpoints implementados.
- [ ] Fluxo pedido → corte → venda → parcela → recebimento executado.
- [ ] Testes críticos registrados com resultado real.
- [ ] Nenhum segredo ou dado real incluído no pacote.
- [ ] README atualizado com pré-requisitos, comandos de execução e endereço da API no MAUI.
- [ ] Limitações e funcionalidades não entregues listadas.
- [ ] Backup/exportação dos dados fictícios e plano para restaurar a demonstração.
- [ ] Confirmar formato exigido: repositório, arquivo compactado, APK, vídeo ou apresentação.
