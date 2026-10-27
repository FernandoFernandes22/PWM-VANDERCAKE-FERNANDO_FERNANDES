param([string]$MySqlBin = 'C:\Program Files\MySQL\MySQL Server 8.0\bin', [int]$Port = 3307, [switch]$ResumeInitialized)
$ErrorActionPreference = 'Stop'
$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$localRoot = Join-Path $projectRoot '.local'
$dataPath = Join-Path $localRoot 'mysql-data'
$server = Join-Path $MySqlBin 'mysqld.exe'
$client = Join-Path $MySqlBin 'mysql.exe'
if (-not (Test-Path -LiteralPath $server)) { throw 'Informe -MySqlBin com o diretório bin do MySQL instalado.' }
[System.IO.Directory]::CreateDirectory($localRoot) | Out-Null
$settingsPath = Join-Path $projectRoot 'src\VanderCake.Api\appsettings.Local.json'
if (Test-Path -LiteralPath $settingsPath) { Write-Output 'Configuração local já existe; nenhum segredo ou banco foi alterado. Use Start-LocalMySql.ps1.'; exit 0 }
if (-not $ResumeInitialized -and (Test-Path -LiteralPath $dataPath) -and (Get-ChildItem -LiteralPath $dataPath -Force | Select-Object -First 1)) { throw 'Diretório de dados já existe sem configuração. Verifique manualmente; nenhum dado será removido.' }
if (-not $ResumeInitialized -and (Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue)) { throw "A porta $Port já está ocupada; escolha outra." }
if (-not $ResumeInitialized) {
$initArgs = @('--no-defaults', '--initialize-insecure', ('--basedir="' + [System.IO.Path]::GetFullPath((Join-Path $MySqlBin '..')) + '"'), ('--datadir="' + $dataPath + '"'), '--console')
$initProcess = Start-Process -FilePath $server -ArgumentList $initArgs -WindowStyle Hidden -PassThru -Wait -RedirectStandardOutput (Join-Path $localRoot 'mysql-initialize.out.log') -RedirectStandardError (Join-Path $localRoot 'mysql-initialize.log')
if ($initProcess.ExitCode -ne 0) { throw 'Falha na inicialização. Consulte .local/mysql-initialize.log.' }
$serverArgs = @('--no-defaults', ('--datadir="' + $dataPath + '"'), "--port=$Port", '--bind-address=127.0.0.1', '--mysqlx=OFF', '--console')
$process = Start-Process -FilePath $server -ArgumentList $serverArgs -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $localRoot 'mysql.out.log') -RedirectStandardError (Join-Path $localRoot 'mysql.err.log')
$process.Id | Set-Content (Join-Path $localRoot 'mysql.pid')
} else {
    $knownPid = [int](Get-Content (Join-Path $localRoot 'mysql.pid'))
    $process = Get-Process -Id $knownPid
    $command = (Get-CimInstance Win32_Process -Filter "ProcessId=$knownPid").CommandLine
    if (-not $command -or -not $command.Contains($dataPath)) { throw 'Não foi possível confirmar que o processo pertence ao banco local deste projeto.' }
}
$ready = $false
for ($attempt=0; $attempt -lt 30; $attempt++) {
    Start-Sleep -Seconds 1
    $ErrorActionPreference = 'Continue'
    & $client --no-defaults -h 127.0.0.1 -P $Port -u root --connect-timeout=1 -e 'SELECT 1' *> $null
    $ErrorActionPreference = 'Stop'
    if ($LASTEXITCODE -eq 0) { $ready=$true; break }
    if ($process.HasExited) { break }
}
if (-not $ready) { throw 'MySQL isolado não iniciou. Consulte os logs em .local.' }
function New-LocalSecret { return ([guid]::NewGuid().ToString('N') + [guid]::NewGuid().ToString('N')) }
$rootPassword = New-LocalSecret
$appPassword = New-LocalSecret
$sql = @"
ALTER USER 'root'@'localhost' IDENTIFIED BY '$rootPassword';
CREATE DATABASE vandercake CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;
CREATE USER 'vandercake'@'localhost' IDENTIFIED BY '$appPassword';
GRANT ALL PRIVILEGES ON vandercake.* TO 'vandercake'@'localhost';
"@
$sql | & $client --no-defaults -h 127.0.0.1 -P $Port -u root
if ($LASTEXITCODE -ne 0) { throw 'Falha ao criar o banco isolado. Não tente reinicializar o diretório de dados.' }
$config = @{ ConnectionStrings=@{ MySql="Server=127.0.0.1;Port=$Port;Database=vandercake;User ID=vandercake;Password=$appPassword" }; Seed=@{ SimpleDemoPasswords=$true; AdminPassword='admin'; SellerPassword='vendedor'; OtherSellerPassword='vendedor2' } }
$config | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $settingsPath -Encoding UTF8
@{ Port=$Port; RootPassword=$rootPassword; MySqlBin=$MySqlBin } | ConvertTo-Json | Set-Content (Join-Path $localRoot 'mysql-admin.json') -Encoding UTF8
Write-Output 'MySQL isolado pronto em 127.0.0.1. Credenciais somente nos arquivos locais ignorados pelo Git. O serviço existente não foi alterado.'
