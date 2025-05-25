param(
    [string]$ManifestPath,
    [string]$Version,
    [string]$InstallerUrl,
    [string]$InstallerPath
)

# Requer o módulo powershell-yaml
if (-not (Get-Module -ListAvailable -Name powershell-yaml)) {
    Install-Module -Name powershell-yaml -Force -Scope CurrentUser
}
Import-Module powershell-yaml

# Carrega o manifesto YAML
$manifest = ConvertFrom-Yaml (Get-Content $ManifestPath -Raw)

# Atualiza os campos principais
$manifest.PackageVersion = $Version
$manifest.Installers[0].InstallerUrl = $InstallerUrl

# Calcula o hash SHA256 do instalador
if (Test-Path $InstallerPath) {
    $sha256 = (Get-FileHash -Path $InstallerPath -Algorithm SHA256).Hash.ToLower()
    $manifest.Installers[0].InstallerSha256 = $sha256
} else {
    Write-Host "Aviso: Instalador não encontrado em $InstallerPath. O campo InstallerSha256 não foi atualizado."
}

# Salva o manifesto atualizado
$manifest | ConvertTo-Yaml | Set-Content $ManifestPath -Encoding UTF8
Write-Host "Manifesto atualizado com sucesso: $ManifestPath"
