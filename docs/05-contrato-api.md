# 05 — Contrato inicial da API

Base: /api/v1. JSON camelCase. Identificadores BIGINT como strings, UUID como strings, instantes ISO 8601 UTC e datas YYYY-MM-DD. Valores monetários JSON numéricos com até duas casas; cálculo no servidor em decimal e exibição BRL no cliente. Respostas paginadas: { items, page, pageSize, totalItems }; page inicia em 1, pageSize padrão 20 e máximo 100.

ADMIN = administrador; VEND = vendedor, sempre limitado à própria carteira/pedidos. Endpoints de escrita com cookie exigem CSRF. Token Bearer MAUI dispensa CSRF, mas não autorização.

## Autenticação

| Método e rota | Acesso | Contrato |
|---|---|---|
| GET /auth/csrf | Público | Obtém token antifalsificação e cookie associado para o painel |
| POST /auth/web/login | Público + CSRF | { login, senha }; cria cookie; retorna usuário/perfil |
| POST /auth/mobile/login | Público | { login, senha }; retorna accessToken, expiresAtUtc e usuário; permitido a vendedor |
| GET /auth/me | Autenticado | Identidade e perfil atuais |
| POST /auth/web/logout | Autenticado + CSRF | Encerra cookie; 204 |

Usuários iniciais criados por seed de desenvolvimento com senha via configuração local. Cadastro público de usuários não faz parte da entrega.

## Cadastros e consultas

| Método e rota | Acesso | Observação |
|---|---|---|
| GET /estados; GET /cidades?estadoId= | ADMIN/VEND | Referências de localização |
| POST/PUT /estados[/id]; /cidades[/id] | ADMIN | Manutenção simples, se necessária além da carga inicial |
| GET/POST /clientes | ADMIN/VEND | GET paginado; POST vendedor recebe vínculo da sessão |
| GET /clientes/{id} | ADMIN/VEND | Verifica carteira |
| PUT /clientes/{id} | ADMIN | Inclui vínculo, situação e ordem de visita |
| GET /vendedores | ADMIN | Usuários com perfil vendedor, sem dados de autenticação |
| GET /carteira | VEND | Lista ordenada do vendedor autenticado |
| GET /clientes/{id}/historico | ADMIN/VEND | Compras concluídas, paginadas |
| GET /clientes/{id}/financeiro | ADMIN/VEND | Saldo vencido/a vencer e parcelas da carteira |
| GET/POST /grupos | GET ambos; POST ADMIN | Listagem e cadastro |
| PUT /grupos/{id} | ADMIN | Edição/inativação |
| GET /produtos | ADMIN/VEND | Filtros texto, grupoId, ativo; paginação |
| GET /produtos/{id} | ADMIN/VEND | Descrição, unidade e preço atual |
| POST /produtos; PUT /produtos/{id} | ADMIN | Não altera estoque diretamente |
| GET/POST /formas-pagamento; PUT /formas-pagamento/{id} | GET ambos; escrita ADMIN | Descrição e situação |
| GET/POST /condicoes-pagamento; PUT /condicoes-pagamento/{id} | GET ambos; escrita ADMIN | Termos e lista de parcelas |
| GET /estoque | ADMIN | Saldo por produto |
| POST /estoque/movimentos | ADMIN | { produtoId, tipo, quantidade, motivo }; entrada/ajuste assinado; saldo validado |

Rotas GET/POST compartilham o caminho; PUT exige ID. Cadastros são inativados, sem DELETE no MVP. Controle de vendedor usa usuários sem um módulo adicional de RH.

## Pedidos

POST /pedidos — VEND — cabeçalho Idempotency-Key: UUID. Sucesso 201 com Location; repetição válida 200 com mesmo pedido.

```json
{
  "clienteId": "10",
  "condicaoPagamentoId": "2",
  "formaPagamentoId": "1",
  "itens": [
    { "produtoId": "7", "quantidade": 5, "precoConsultado": 12.50 }
  ]
}
```

Servidor limita 100 itens, valida carteira e termos, resolve vendedor e calcula totais. Não aceita quantidade aprovada, vendedorId ou total como autoridade.

| Método e rota | Acesso | Resultado |
|---|---|---|
| GET /pedidos?situacao=&clienteId=&de=&ate= | ADMIN/VEND | Lista no escopo do usuário |
| GET /pedidos/{id} | ADMIN/VEND | Itens solicitados/aprovados, totais e vínculos |
| GET /pedidos/por-chave/{uuid} | VEND | Pedido da própria tentativa; 404 se ainda não existe |
| POST /pedidos/{id}/cancelamento | ADMIN | { motivo }; somente ENVIADO |

GET por chave não confirma que uma solicitação concorrente deixou de processar: após timeout, repetir a mesma chave/conteúdo continua sendo a operação segura.

## Atendimento e vendas

| Método e rota | Acesso | Contrato |
|---|---|---|
| POST /atendimentos/previa | ADMIN | { pedidoIds: ["1", "2"] }; retorna previewToken, expiresAtUtc, produtos/saldos e distribuição por cliente/pedido |
| POST /atendimentos/confirmacao | ADMIN | Idempotency-Key; { previewToken }; retorna loteId, pedidos, vendas e parcelas geradas |
| GET /atendimentos/{id} | ADMIN | Resultado persistido da rodada |
| GET /vendas; GET /vendas/{id} | ADMIN | Consulta com filtros por cliente e data |

Limite inicial: 100 pedidos por prévia. Token vale dez minutos como proposta técnica; revalidação de dados é obrigatória mesmo dentro do prazo. Estoque ou pedido alterado retorna 409 PREVIA_DESATUALIZADA. Resposta não efetua atendimento parcial da rodada em caso de falha.

## Financeiro

| Método e rota | Acesso | Contrato |
|---|---|---|
| GET /contas-receber | ADMIN | Filtros clienteId, situacao, vencimentoDe/Ate e atrasada |
| GET /contas-receber/{id} | ADMIN | Parcela, saldo e recebimentos |
| POST /contas-receber/{id}/recebimentos | ADMIN | Idempotency-Key; { valor, dataRecebimento }; cria recebimento e retorna novo saldo |
| GET /contas-receber/{id}/simulacao?dataRecebimento=YYYY-MM-DD | ADMIN | Principal em aberto, juros, desconto, total, dias antecipados/atrasados |

Atualização de 26/10/2026: TermsInput/TermsDto incluem jurosDiaPercentual e descontoDiaPercentual. PUT de condição está implementado; taxas ficam congeladas no pedido. O recebimento aceita saldoConferido opcional, usado pelo painel para rejeitar prévia desatualizada. As contas retornam saldoPrincipal, saldo atualizado, juros, desconto e histórico com principalBaixado/ajustes. Detalhes das fórmulas no documento 03.

## Erros

Usar application/problem+json, sem detalhes internos. Exemplo:

```json
{
  "type": "about:blank",
  "title": "O preço de um produto mudou.",
  "status": 409,
  "code": "PRECO_ALTERADO",
  "traceId": "identificador-de-correlacao",
  "errors": { "itens[0].precoConsultado": ["Atualize o preço e confirme novamente."] }
}
```

400 validação/formato; 401 sessão ausente/expirada; 403 perfil sem permissão; 404 inexistente ou fora da carteira; 409 conflito de preço/estado/estoque/idempotência; 500 falha inesperada com mensagem genérica. Clientes distinguem falta de conexão de erro de regra.

Gerar OpenAPI durante a implementação e conferir que contratos publicados correspondem a este documento. Não presumir que este arquivo já seja uma especificação OpenAPI executável.
