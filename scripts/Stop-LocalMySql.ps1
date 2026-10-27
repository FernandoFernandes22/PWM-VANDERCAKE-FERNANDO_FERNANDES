$ErrorActionPreference = 'Stop'
$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$localRoot = Join-Path $projectRoot '.local'
$config = Get-Content -LiteralPath (Join-Path $localRoot 'mysql-admin.json') -Raw | ConvertFrom-Json
$client = Join-Path $config.MySqlBin 'mysql.exe'
$defaultsPath = Join-Path $localRoot 'mysql-shutdown.cnf'
$contents = "[client]`nuser=root`npassword=$($config.RootPassword)`nhost=127.0.0.1`nport=$($config.Port)`n"
try {
    [System.IO.File]::WriteAllText($defaultsPath, $contents, [System.Text.UTF8Encoding]::new($false))
    # The credential only applies to this project's private instance.
    & $client "--defaults-extra-file=$defaultsPath" -e 'SHUTDOWN'
    if ($LASTEXITCODE -ne 0) { throw 'Não foi possível encerrar o banco isolado.' }
} finally { if (Test-Path -LiteralPath $defaultsPath) { Remove-Item -LiteralPath $defaultsPath } }
Write-Output 'Banco isolado do VanderCake encerrado; o serviço MySQL existente não foi alterado.'
