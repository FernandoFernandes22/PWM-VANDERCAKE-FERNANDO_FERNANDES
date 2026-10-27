param([string]$BaseUrl = 'http://127.0.0.1:5369')
$ErrorActionPreference = 'Stop'
if (([uri]$BaseUrl).Port -eq 5268) { throw 'Use uma API e banco de validação separados. O banco da apresentação não deve receber os testes automatizados.' }
$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$config = Get-Content -LiteralPath (Join-Path $projectRoot 'src\VanderCake.Api\appsettings.Local.json') -Raw | ConvertFrom-Json
$script:checks = 0
function Check($condition, [string]$name) { if (-not $condition) { throw "Falhou: $name" }; $script:checks++; Write-Output "OK: $name" }
function Call-Api([string]$path, [string]$method='GET', $body=$null, $headers=@{}, $session=$null) {
    $params = @{ Uri="$BaseUrl/api/v1$path"; Method=$method; Headers=$headers; ContentType='application/json' }
    if ($null -ne $body) { $params.Body=($body | ConvertTo-Json -Depth 20 -Compress) }
    if ($session) { $params.WebSession=$session }
    $result = Invoke-RestMethod @params
    return $result
}
$admin = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$csrf = Call-Api '/auth/csrf' 'GET' $null @{} $admin
$null = Call-Api '/auth/web/login' 'POST' @{login='admin';senha=$config.Seed.AdminPassword} @{'X-CSRF-TOKEN'=$csrf.token} $admin
$csrf = Call-Api '/auth/csrf' 'GET' $null @{} $admin
$adminHeaders = @{'X-CSRF-TOKEN'=$csrf.token}
$seller = Call-Api '/auth/mobile/login' 'POST' @{login='vendedor';senha=$config.Seed.SellerPassword}
$sellerHeaders = @{Authorization="Bearer $($seller.accessToken)"}
$other = Call-Api '/auth/mobile/login' 'POST' @{login='vendedor2';senha=$config.Seed.OtherSellerPassword}
$otherHeaders = @{Authorization="Bearer $($other.accessToken)"}
Check ((Call-Api '/auth/me' 'GET' $null $sellerHeaders).perfil -eq 'VENDEDOR') 'login mobile e identidade'
$clients = @(Call-Api '/carteira' 'GET' $null $sellerHeaders)
$otherClients = @(Call-Api '/carteira' 'GET' $null $otherHeaders)
Check ($clients.Count -ge 3 -and $otherClients.Count -ge 1) 'carteiras independentes'
try { $null=Call-Api "/clientes/$($clients[0].id)" 'GET' $null $otherHeaders; throw 'Isolamento falhou' } catch { Check ($_.Exception.Response.StatusCode.value__ -eq 404) 'cliente alheio retorna 404' }
$suffix=[guid]::NewGuid().ToString('N').Substring(0,8)
$product=Call-Api '/produtos' 'POST' @{codigo="TEST-$suffix";descricao="Produto teste $suffix";grupo='Testes';unidade='UN';preco=10;ativo=$true} $adminHeaders $admin
$null=Call-Api '/estoque/movimentos' 'POST' @{produtoId=$product.id;quantidade=15;motivo='Teste de integração'} $adminHeaders $admin
$terms=Call-Api '/condicoes-pagamento' 'POST' @{nome="Condição de teste $suffix";parcelas=@(@{numero=1;dias=7;percentual=50},@{numero=2;dias=14;percentual=50});jurosDiaPercentual=0;descontoDiaPercentual=0} $adminHeaders $admin
$method=@(Call-Api '/formas-pagamento' 'GET' $null $sellerHeaders) | Select-Object -First 1
$orders=@();$amounts=@(5,8,9)
for($i=0;$i -lt 3;$i++) {
    $key=[guid]::NewGuid().ToString();$headers=@{Authorization=$sellerHeaders.Authorization;'Idempotency-Key'=$key}
    $payload=@{clienteId=$clients[$i].id;condicaoPagamentoId=$terms.id;formaPagamentoId=$method.id;itens=@(@{produtoId=$product.id;quantidade=$amounts[$i];precoConsultado=10})}
    $order=Call-Api '/pedidos' 'POST' $payload $headers
    $repeat=Call-Api '/pedidos' 'POST' $payload $headers
    Check ($order.id -eq $repeat.id) "envio idempotente do pedido $($i+1)"
    $orders+=,$order
}
$preview=Call-Api '/atendimentos/previa' 'POST' @{pedidoIds=@($orders | ForEach-Object {$_.id})} $adminHeaders $admin
$approved=@($preview.itens | Sort-Object {[long]$_.pedidoId} | ForEach-Object {$_.aprovado})
Check (($approved -join ',') -eq '3,6,6') 'rateio proporcional 3/6/6'
$confirmHeaders=@{'X-CSRF-TOKEN'=$csrf.token;'Idempotency-Key'=[guid]::NewGuid().ToString()}
$batch=Call-Api '/atendimentos/confirmacao' 'POST' @{previewToken=$preview.previewToken} $confirmHeaders $admin
$repeat=Call-Api '/atendimentos/confirmacao' 'POST' @{previewToken=$preview.previewToken} $confirmHeaders $admin
Check ($batch.loteId -eq $repeat.loteId -and $batch.vendaIds.Count -eq 3) 'confirmação idempotente e três vendas'
$stock=@(Call-Api '/estoque' 'GET' $null $adminHeaders $admin) | Where-Object {$_.produtoId -eq $product.id}
Check ($stock.saldo -eq 0) 'estoque baixado uma única vez'
$sale=Call-Api "/vendas/$($batch.vendaIds[0])" 'GET' $null $adminHeaders $admin
Check (($sale.parcelas | Measure-Object -Property valor -Sum).Sum -eq $sale.total) 'parcelas fecham o total'
$account=$sale.parcelas[0]
$receiptHeaders=@{'X-CSRF-TOKEN'=$csrf.token;'Idempotency-Key'=[guid]::NewGuid().ToString()}
$today=[System.TimeZoneInfo]::ConvertTimeBySystemTimeZoneId([datetime]::UtcNow,'E. South America Standard Time').ToString('yyyy-MM-dd')
$payload=@{valor=5;dataRecebimento=$today}
$receipt=Call-Api "/contas-receber/$($account.id)/recebimentos" 'POST' $payload $receiptHeaders $admin
$receiptAgain=Call-Api "/contas-receber/$($account.id)/recebimentos" 'POST' $payload $receiptHeaders $admin
Check ($receipt.recebimentoId -eq $receiptAgain.recebimentoId) 'recebimento idempotente'
$accountResult=Call-Api "/contas-receber/$($account.id)" 'GET' $null $adminHeaders $admin
Check ($accountResult.saldo -eq ($account.valor-5) -and $accountResult.situacao -eq 'PARCIAL') 'pagamento parcial atualiza saldo'
$history=Call-Api "/clientes/$($orders[0].clienteId)/historico" 'GET' $null $sellerHeaders
Check ($history.totalItems -ge 1) 'vendedor consulta histórico'
try { $null=Call-Api '/produtos' 'POST' @{codigo="NEG-$suffix"} @{} $admin; throw 'CSRF falhou' } catch { Check ($_.Exception.Response.StatusCode.value__ -eq 400) 'escrita administrativa exige CSRF' }
function New-TestProduct([int]$stock) {
    $code='TEST-'+[guid]::NewGuid().ToString('N').Substring(0,10)
    $p=Call-Api '/produtos' 'POST' @{codigo=$code;descricao=$code;grupo='Testes';unidade='UN';preco=10;ativo=$true} $adminHeaders $admin
    if($stock -gt 0) { $null=Call-Api '/estoque/movimentos' 'POST' @{produtoId=$p.id;quantidade=$stock;motivo='Teste de concorrência'} $adminHeaders $admin }
    return $p
}
function New-TestOrder($productId,$clientId,[int]$quantity) {
    Call-Api '/pedidos' 'POST' @{clienteId=$clientId;condicaoPagamentoId=$terms.id;formaPagamentoId=$method.id;itens=@(@{produtoId=$productId;quantidade=$quantity;precoConsultado=10})} @{Authorization=$sellerHeaders.Authorization;'Idempotency-Key'=[guid]::NewGuid().ToString()}
}
function Send-Concurrently([string]$path1,$body1,[string]$path2,$body2) {
    Add-Type -AssemblyName System.Net.Http
    $handler=New-Object System.Net.Http.HttpClientHandler
    $handler.CookieContainer=$admin.Cookies
    $http=New-Object System.Net.Http.HttpClient($handler)
    $requests=@();$tasks=@()
    foreach($entry in @(@{path=$path1;body=$body1},@{path=$path2;body=$body2})) {
        $request=New-Object System.Net.Http.HttpRequestMessage([System.Net.Http.HttpMethod]::Post,"$BaseUrl/api/v1$($entry.path)")
        $request.Headers.Add('X-CSRF-TOKEN',$csrf.token)
        $request.Headers.Add('Idempotency-Key',[guid]::NewGuid().ToString())
        $request.Content=New-Object System.Net.Http.StringContent(($entry.body | ConvertTo-Json -Depth 20 -Compress),[System.Text.Encoding]::UTF8,'application/json')
        $requests+=,$request;$tasks+=,$http.SendAsync($request)
    }
    $responses=@();foreach($task in $tasks) { $responses+=,$task.GetAwaiter().GetResult() }
    $statuses=@($responses | ForEach-Object { [int]$_.StatusCode })
    foreach($response in $responses) { $response.Dispose() };foreach($request in $requests) { $request.Dispose() };$http.Dispose()
    return $statuses
}
$p=New-TestProduct 10
$o=New-TestOrder $p.id $clients[0].id 10
$stale=Call-Api '/atendimentos/previa' 'POST' @{pedidoIds=@($o.id)} $adminHeaders $admin
$null=Call-Api '/estoque/movimentos' 'POST' @{produtoId=$p.id;quantidade=1;motivo='Invalidar prévia para teste'} $adminHeaders $admin
try { $null=Call-Api '/atendimentos/confirmacao' 'POST' @{previewToken=$stale.previewToken} @{'X-CSRF-TOKEN'=$csrf.token;'Idempotency-Key'=[guid]::NewGuid().ToString()} $admin;throw 'Prévia desatualizada foi aceita' } catch { Check ($_.Exception.Response.StatusCode.value__ -eq 409) 'prévia desatualizada rejeitada' }
Check ((Call-Api "/pedidos/$($o.id)" 'GET' $null $sellerHeaders).situacao -eq 'ENVIADO') 'rejeição preserva o pedido sem venda'

$p=New-TestProduct 10
$o1=New-TestOrder $p.id $clients[0].id 10
$o2=New-TestOrder $p.id $clients[1].id 10
$pre1=Call-Api '/atendimentos/previa' 'POST' @{pedidoIds=@($o1.id)} $adminHeaders $admin
$pre2=Call-Api '/atendimentos/previa' 'POST' @{pedidoIds=@($o2.id)} $adminHeaders $admin
$statuses=@(Send-Concurrently '/atendimentos/confirmacao' @{previewToken=$pre1.previewToken} '/atendimentos/confirmacao' @{previewToken=$pre2.previewToken})
Check (($statuses | Sort-Object) -join ',' -eq '200,409') 'atendimentos concorrentes: um sucesso e um conflito'
$stock=@(Call-Api '/estoque' 'GET' $null $adminHeaders $admin) | Where-Object {$_.produtoId -eq $p.id}
Check ($stock.saldo -eq 0) 'concorrência não produz estoque negativo'
$processed=@((Call-Api "/pedidos/$($o1.id)" 'GET' $null $sellerHeaders),(Call-Api "/pedidos/$($o2.id)" 'GET' $null $sellerHeaders))
Check (@($processed | Where-Object {$_.situacao -eq 'ATENDIDO'}).Count -eq 1) 'somente um pedido concorrente atendido'

$newSale=@(Call-Api '/vendas?pageSize=100' 'GET' $null $adminHeaders $admin).items | Where-Object {$_.pedidoId -eq ($processed | Where-Object {$_.situacao -eq 'ATENDIDO'}).id} | Select-Object -First 1
$newSale=Call-Api "/vendas/$($newSale.id)" 'GET' $null $adminHeaders $admin
$account=$newSale.parcelas[0]
$path="/contas-receber/$($account.id)/recebimentos"
$statuses=@(Send-Concurrently $path @{valor=40;dataRecebimento=$today} $path @{valor=40;dataRecebimento=$today})
Check (($statuses | Sort-Object) -join ',' -eq '200,409') 'recebimentos concorrentes não excedem saldo'
$accountResult=Call-Api "/contas-receber/$($account.id)" 'GET' $null $adminHeaders $admin
Check ($accountResult.recebido -eq 40 -and $accountResult.saldo -eq 10) 'saldo correto após recebimentos concorrentes'

$p=New-TestProduct 0
$o=New-TestOrder $p.id $clients[0].id 5
$pre=Call-Api '/atendimentos/previa' 'POST' @{pedidoIds=@($o.id)} $adminHeaders $admin
$result=Call-Api '/atendimentos/confirmacao' 'POST' @{previewToken=$pre.previewToken} @{'X-CSRF-TOKEN'=$csrf.token;'Idempotency-Key'=[guid]::NewGuid().ToString()} $admin
Check ($result.vendaIds.Count -eq 0 -and (Call-Api "/pedidos/$($o.id)" 'GET' $null $sellerHeaders).situacao -eq 'NAO_ATENDIDO') 'corte total não gera venda ou parcelas'

$p=New-TestProduct 2
$splitOrders=@((New-TestOrder $p.id $clients[0].id 1),(New-TestOrder $p.id $clients[0].id 1),(New-TestOrder $p.id $clients[1].id 1))
$pre=Call-Api '/atendimentos/previa' 'POST' @{pedidoIds=@($splitOrders | ForEach-Object {$_.id})} $adminHeaders $admin
$clientShare=($pre.itens | Where-Object {$_.clienteId -eq $clients[0].id} | Measure-Object -Property aprovado -Sum).Sum
Check ($clientShare -eq 1 -and ($pre.itens | Measure-Object -Property aprovado -Sum).Sum -eq 2) 'fracionar pedidos não aumenta a participação do cliente'
Write-Output "Integração concluída: $script:checks verificações. Dados de teste foram mantidos para consulta."
