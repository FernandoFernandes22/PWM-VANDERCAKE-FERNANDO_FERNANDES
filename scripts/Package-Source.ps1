$ErrorActionPreference = 'Stop'
$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$outputDirectory = Join-Path $projectRoot 'outputs'
[System.IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
$output = Join-Path $outputDirectory 'VanderCake-codigo.zip'
Add-Type -AssemblyName System.IO.Compression
$stream = [System.IO.File]::Open($output, [System.IO.FileMode]::Create)
$archive = [System.IO.Compression.ZipArchive]::new($stream, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    $files = Get-ChildItem -LiteralPath $projectRoot -File -Recurse -Force | Where-Object {
        $_.FullName -notmatch '[\\/](bin|obj|\.local|\.git|\.vs|TestResults|outputs)[\\/]' -and $_.Name -ne 'appsettings.Local.json' -and $_.Extension -notin @('.zip','.apk','.keystore','.db3','.user')
    }
    foreach ($file in $files) {
        $relative = $file.FullName.Substring($projectRoot.Length + 1).Replace('\','/')
        $entry=$archive.CreateEntry('VanderCake/'+$relative,[System.IO.Compression.CompressionLevel]::Optimal)
        $entryStream=$entry.Open();$source=[System.IO.File]::OpenRead($file.FullName)
        try { $source.CopyTo($entryStream) } finally { $source.Dispose();$entryStream.Dispose() }
    }
} finally { $archive.Dispose();$stream.Dispose() }
Write-Output "Código empacotado em $output, sem bancos locais, credenciais ou arquivos de compilação."
