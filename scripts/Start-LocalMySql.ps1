$ErrorActionPreference = 'Stop'
$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$localRoot = Join-Path $projectRoot '.local'
$config = Get-Content -LiteralPath (Join-Path $localRoot 'mysql-admin.json') -Raw | ConvertFrom-Json
if (Get-NetTCPConnection -LocalPort $config.Port -State Listen -ErrorAction SilentlyContinue) { Write-Output 'A porta do banco já está em uso. Nenhum outro processo será iniciado.'; exit 0 }
$server = Join-Path $config.MySqlBin 'mysqld.exe'
$arguments = @('--no-defaults', ('--datadir="' + (Join-Path $localRoot 'mysql-data') + '"'), "--port=$($config.Port)", '--bind-address=127.0.0.1', '--mysqlx=OFF', '--console')
$process = Start-Process -FilePath $server -ArgumentList $arguments -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $localRoot 'mysql.out.log') -RedirectStandardError (Join-Path $localRoot 'mysql.err.log')
$process.Id | Set-Content (Join-Path $localRoot 'mysql.pid')
Write-Output 'Inicialização do banco isolado solicitada.'
