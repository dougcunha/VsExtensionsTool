# Installs Inno Setup using Chocolatey if not already installed
if (-not (Get-Command iscc.exe -ErrorAction SilentlyContinue)) {
    choco install innosetup --yes
}
