# 03 — Regras de negócio

## Convenções

- Distribuidora única, moeda BRL e datas de negócio no fuso America/Sao_Paulo como premissa inicial.
- Instantes de auditoria em UTC; vencimentos como data sem horário.
- Primeira versão vende unidades inteiras: caixa, pacote ou unidade são unidades comerciais indivisíveis. Venda por peso/fracionada requer revisão explícita.
- Quantidades positivas inteiras; preços e valores em decimal com duas casas; percentuais em decimal com duas casas.
- Dinheiro: arredondamento para centavo com midpoint away from zero, de forma explícita e consistente.

## Cliente, carteira e roteiro

Cada cliente possui um vendedor responsável. Administrador atribui e altera o vínculo. Vendedor pode criar cliente, mas não escolher outro responsável. Histórico anterior permanece associado ao vendedor original do pedido. Não transferir cliente com pedido ENVIADO: concluir ou cancelar o pedido antes da transferência.

Roteiro inicial é a ordem fixa dos clientes na carteira; não é mapa nem agenda de visitas realizadas. Empates de ordem são resolvidos pelo ID do cliente.

## Preços

O servidor fornece preço atual. O envio inclui preço visto pelo vendedor apenas para detectar alterações. Se houver divergência, retornar conflito e solicitar confirmação dos valores atualizados; não alterar silenciosamente. Preço unitário aceito é congelado no item do pedido. Histórico usa o preço efetivamente vendido. Sem negociação de preços/descontos no MVP.

## Pedido

RASCUNHO existe no SQLite. Ao enviar, o servidor cria ENVIADO. Depois: ATENDIDO, NAO_ATENDIDO ou CANCELADO.

- Apenas ENVIADO pode ser selecionado para atendimento ou cancelado pelo administrador.
- Vendedor não altera pedido enviado; correção é cancelamento administrativo e novo pedido.
- Quantidade aprovada igual a zero em todos os itens resulta em NAO_ATENDIDO, sem venda ou parcelas.
- Atendimento parcial encerra o pedido: itens cortados não viram pendência automática.
- Produto repetido no mesmo pedido é rejeitado; a interface deve somar a quantidade na linha existente.
- Cliente, produto, condição e forma precisam estar ativos no envio.
- Um pedido define uma condição e uma forma de pagamento. Não dividir formas por parcela nesta versão.

## Envio idempotente

Rascunho recebe UUID persistente. Ao enviar, esse UUID vai em Idempotency-Key e fica vinculado ao usuário e ao conteúdo da tentativa. Repetir chave/conteúdo devolve o pedido original. Reutilizar chave com conteúdo diferente retorna conflito. Em timeout, consultar/repetir a mesma tentativa antes de permitir nova submissão; editar conteúdo requer nova chave após resolver a tentativa anterior.

## Estoque

Saldo oficial no MySQL. Envio de pedido não reserva saldo. Entrada/ajuste manual requer motivo e administrador. Ajuste não pode tornar o saldo negativo. Atendimento gera saída vinculada à venda. Registrar saldo inicial por movimento, não diretamente por edição do produto.

## Lote e corte proporcional

Administrador seleciona explicitamente pedidos ENVIADOS de uma mesma rodada. Todo estoque disponível do produto pode atender essa rodada. A tela exibe pedidos e clientes incluídos antes de confirmar; pedidos fora do lote não participam do rateio.

Calcular por produto:

1. Agrupar pedidos por cliente e somar quantidades solicitadas. Isso impede vantagem por fracionar um pedido em vários.
2. Se estoque >= demanda, aprovar tudo. Se estoque = 0, aprovar zero.
3. Caso contrário, para cada cliente: quota = estoque × demanda do cliente / demanda total.
4. Atribuir a parte inteira da quota.
5. Distribuir unidades restantes por maior resto decimal. Empate: pedido mais antigo do cliente no lote; depois ID do cliente.
6. Distribuir a quantidade atribuída ao cliente entre seus pedidos em ordem de envio e ID, sem ultrapassar a solicitação de cada um.

Comparações de resto devem ser exatas: utilizar numeradores inteiros e o mesmo denominador, evitando erros de ponto flutuante.

Exemplo com estoque 15 e demandas A=5, B=8, C=9: partes inteiras 3, 5, 6; sobra 1 vai a B, maior resto. Resultado 3, 6, 6. O exemplo das fotos não fixa esse resultado.

Prévia não reserva estoque. API emite identificador/token opaco vinculado ao conjunto de pedidos, versões dos dados e saldos usados. Na confirmação, revalidar tudo sob transação; se houve mudança, responder 409 e exigir nova prévia. Não recalcular e confirmar silenciosamente outro resultado. Não permitir ajuste manual das quantidades proporcionais no MVP.

Confirmar a rodada deve, atomicamente: validar estados e saldos, gravar lote/itens, registrar aprovados, baixar estoque, concluir pedidos, gerar vendas e parcelas. Usar bloqueio de linhas ou atualização condicional compatível com o provedor, em ordem consistente de produtos para reduzir deadlocks. Constraint única impede venda duplicada. Repetição da confirmação retorna o mesmo resultado.

## Venda

Premissa: venda nasce na confirmação do atendimento. Não há etapa logística separada nesta versão. Total = soma de quantidade aprovada × preço congelado, em centavos. Pedido totalmente cortado não gera venda. Venda registra condição e forma congeladas; editar cadastros não altera vendas já concluídas.

## Financeiro

- Condição possui nome e parcelas com número, dias após venda e percentual. Dias >= 0, números sequenciais, percentuais positivos e soma exata de 100%.
- Forma identifica a modalidade (ex.: boleto, dinheiro, PIX); é apenas registro, sem integração.
- Vencimento = data da venda + dias corridos; sem ajuste de feriados/fins de semana no MVP.
- Para cada parcela exceto a última, arredondar total × percentual / 100. Última recebe o restante. Rejeitar configuração que gere parcela de valor zero para a venda.
- Exemplo: R$ 235,00 em 50%/50% gera R$ 117,50 e R$ 117,50.
- Exemplo: R$ 100,00 em 33,33%/33,33%/33,34% gera R$ 33,33, R$ 33,33 e R$ 33,34.
- Recebimento pode ser parcial ou integral, positivo e não superior ao saldo. Data não pode ser futura; deve ser igual ou posterior à venda.
- Não excluir recebimentos, cancelar venda ou estornar estoque na primeira versão. Erros após confirmação são limitação documentada e exigem evolução específica.
- Principal em aberto = valor original menos principal amortizado. O saldo atualizado inclui juros ou desconto na data de consulta; o recebido é o dinheiro efetivo. Estado derivado: ABERTA, PARCIAL ou PAGA. Atraso é indicador separado para principal > 0 e vencimento anterior à data atual.
- Débito do cliente inclui parcelas abertas vencidas e a vencer, apresentadas separadamente.

## Multa, juros e desconto

Regra confirmada pelo usuário em 26/10/2026: taxas configuráveis na condição, congeladas no pedido e copiadas às parcelas na confirmação. Padrão de demonstração: juros compostos de 2% por dia de atraso; desconto composto de 1% por dia antecipado. Dias corridos, sem carência, sem multa fixa adicional. No vencimento não há ajuste.

- Atraso: principal em aberto × (1 + taxa/100)^dias.
- Antecipação: principal em aberto × (1 − taxa/100)^dias.
- Usar decimal e arredondamento final para centavos, afastando do zero em empates. Uma dívida positiva mantém o mínimo de R$0,01 para quitação; crescimento acima do limite decimal do banco é rejeitado.
- Parcial: principal amortizado = principal em aberto × recebido / saldo atualizado, arredondado em centavos. Juros ou desconto efetivos são a diferença entre recebido e principal amortizado. Na quitação, baixa-se exatamente todo o principal restante para não deixar resíduos. O saldo nominal remanescente continua sujeito ao fator composto desde o vencimento.
- Guardar recebido, principal baixado, juros, desconto e data no recebimento. Datas posteriores a hoje, anteriores à venda ou a um recebimento já registrado na mesma parcela são rejeitadas.
- A tela consulta a prévia da API para a data escolhida. Ao confirmar, o servidor recalcula em transação, rejeita saldo conferido desatualizado e mantém idempotência.
- Alterações de taxas não são retroativas. Pedidos e parcelas anteriores à migração permanecem com taxas zero; recebimentos antigos são migrados como baixa integral de principal.
