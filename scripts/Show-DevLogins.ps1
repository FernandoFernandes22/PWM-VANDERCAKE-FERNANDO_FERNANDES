$ErrorActionPreference = 'Stop'
$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$config = Get-Content -LiteralPath (Join-Path $projectRoot 'src\VanderCake.Api\appsettings.Local.json') -Raw | ConvertFrom-Json
Write-Host 'Credenciais fictícias locais. Não compartilhe este resultado ou a configuração local.'
Write-Host "Administrador: admin / $($config.Seed.AdminPassword)"
Write-Host "Vendedor: vendedor / $($config.Seed.SellerPassword)"
Write-Host "Outra carteira: vendedor2 / $($config.Seed.OtherSellerPassword)"
