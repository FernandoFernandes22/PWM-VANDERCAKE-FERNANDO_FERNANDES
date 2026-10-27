# 06 — Telas e fluxos

## Aplicativo MAUI

| Tela | Conteúdo e ações |
|---|---|
| Login | Login/senha, indicador de conexão e erro; redirecionar para carteira ao autenticar |
| Carteira | Busca por cliente, ordem de visita, cidade e ação de novo cliente |
| Novo cliente | Nome e endereço/localização obrigatórios; contato/documento opcionais; responsável vem da sessão |
| Cliente | Dados, última compra, histórico, débitos vencidos/a vencer e botão Novo pedido |
| Catálogo | Busca, grupo, descrição, unidade, preço e quantidade; não prometer estoque disponível ao vendedor |
| Rascunho | Itens, quantidades, total estimado, condição, forma e botão Enviar pedido |
| Rascunhos | Pedidos locais do usuário, retomada e exclusão apenas de EDITANDO |
| Meus pedidos | Data, cliente, situação e total; filtros simples |
| Detalhe do pedido | Número, solicitado/aprovado, corte, valores e situação |

Na montagem, salvar rascunho ao alterar itens/termos. Mostrar “Rascunho salvo neste aparelho”. Antes do envio, confirmar cliente, itens e pagamento. Desabilitar botão durante a chamada e manter proteção no servidor. Sucesso mostra número do pedido. Timeout mostra “Confirmando envio”; reutilizar tentativa ao tentar novamente.

Sem internet: indicar que cache pode estar desatualizado; permitir visualizar/editar rascunho já existente como conveniência local, mas bloquear envio e consultas que dependam do servidor. Isso não constitui um modo offline completo.

## Painel HTML/CSS/JavaScript

Menu: Pedidos, Clientes, Produtos, Estoque, Pagamentos, Vendas e Contas a receber. Grupos/localização/vendedores podem ficar como seções de Cadastros. Tela inicial abre pedidos pendentes; dashboard com gráficos não é necessário.

| Tela | Conteúdo e ações |
|---|---|
| Login | Acesso administrativo e feedback de erro |
| Clientes | Lista, cadastro/edição, responsável, ordem e histórico |
| Produtos e grupos | Lista, preço, unidade, situação e edição |
| Estoque | Saldo e registro de entrada/ajuste com motivo |
| Condições e formas | Cadastro de termos, parcelas, soma percentual e modalidades |
| Pedidos | Filtros, seleção de rodada, total solicitado e prévia do atendimento |
| Prévia do corte | Estoque, demanda, clientes incluídos, quantidades solicitadas/aprovadas e confirmação |
| Resultado | Número do lote, vendas criadas e pedidos não atendidos |
| Vendas | Consulta do pedido de origem, itens, termos e parcelas |
| Contas a receber | Filtros, saldo, atraso, detalhe e registro de pagamento |

## Estados de interface obrigatórios

- Carregando: indicador e bloqueio apenas das ações conflitantes.
- Vazio: mensagem específica e ação pertinente.
- Erro de campo: mensagem próxima ao campo e preservação dos dados digitados.
- Sessão expirada: voltar ao login; preservar rascunho do mesmo usuário no aplicativo.
- Sem permissão: informar acesso indisponível, sem exibir dados retornados anteriormente de outro perfil.
- Prévia desatualizada: exigir novo cálculo e nova conferência.
- Sucesso: mostrar identificador do recurso e ação de consulta.

## Conteúdo e acessibilidade

Português brasileiro, moeda em R$, datas DD/MM/AAAA na tela e sem termos técnicos como endpoint/DTO para usuários. Formulários com labels, foco visível, mensagens legíveis e navegação por teclado no painel. Situações usam texto além de cores. Campos monetários distinguem vírgula de apresentação e formato enviado à API.

## Roteiro da demonstração

Login vendedor → carteira → cliente → rascunho salvo → reabrir app → enviar pedido → login administrador → selecionar pedidos de três clientes → visualizar corte → confirmar → consultar venda/parcelas → registrar pagamento parcial → vendedor consulta saldo atualizado.
