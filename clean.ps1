# Limpeza de binários e arquivos temporários do projeto VsExtensionsTool
# Uso: Execute este script no PowerShell na raiz do repositório

Remove-Item -Recurse -Force .\VsExtensionsTool\bin, .\VsExtensionsTool\obj, .\VsExtensionsTool.Tests\bin, .\VsExtensionsTool.Tests\obj, .\VsExtensionsTool\Managers\bin, .\VsExtensionsTool\Managers\obj, .\VsExtensionsTool\Helpers\bin, .\VsExtensionsTool\Helpers\obj
Write-Host 'Pastas bin e obj removidas de ambos os projetos.' -ForegroundColor Green
