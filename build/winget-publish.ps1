param(
    [string]$WingetExe,
    [string]$PackageId,
    [string]$InstallerUrl,
    [string]$Version,
    [string]$PrTitle,
    [string]$Token
)

# Tenta buscar o manifesto remoto
try {
    $url = "https://api.github.com/repos/microsoft/winget-pkgs/contents/manifests/" + $PackageId.ToLower().Replace('.', '/')
    $response = Invoke-RestMethod -Uri $url -Headers @{ 'Accept' = 'application/vnd.github.v3+json' }
    $exists = $true
} catch {
    $exists = $false
}

if ($exists) {
    Write-Host "Manifesto remoto encontrado. Usando 'update'."
    & $WingetExe update --submit --token $Token --urls $InstallerUrl --version $Version --prtitle $PrTitle --out manifests $PackageId
    exit $LASTEXITCODE
} else {
    Write-Host "Manifesto remoto NÃO encontrado. Usando 'new'."
    & $WingetExe new --submit --token $Token --urls $InstallerUrl --version $Version --prtitle $PrTitle --out manifests
    exit $LASTEXITCODE
}
