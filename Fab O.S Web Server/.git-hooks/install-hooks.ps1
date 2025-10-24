# Fab.OS Platform - Git Hooks Installation Script (PowerShell)
# Installs pre-commit hooks for URL architecture validation

Write-Host "🔧 Installing Fab.OS Git Hooks" -ForegroundColor Cyan
Write-Host "===============================" -ForegroundColor Cyan
Write-Host ""

# Check if .git directory exists
if (-not (Test-Path ".git")) {
    Write-Host "❌ ERROR: Not a git repository" -ForegroundColor Red
    Write-Host "   Run this script from the root of your git repository"
    exit 1
}

# Create hooks directory if it doesn't exist
New-Item -ItemType Directory -Force -Path ".git/hooks" | Out-Null

# Copy pre-commit hook
Write-Host "📋 Installing pre-commit hook..."
Copy-Item ".git-hooks/pre-commit" ".git/hooks/pre-commit" -Force

# Create a Windows-compatible wrapper if on Windows
if ($IsWindows -or $env:OS -match "Windows") {
    Write-Host "📝 Creating Windows wrapper..."

    $wrapperContent = @'
#!/bin/bash
# Windows-compatible pre-commit hook wrapper

# Convert line endings if needed
if command -v dos2unix &> /dev/null; then
    dos2unix "$0" 2>/dev/null || true
fi

# Get the directory of this script
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
HOOK_SCRIPT="$SCRIPT_DIR/../../.git-hooks/pre-commit"

# Fix line endings in the actual hook script
if [ -f "$HOOK_SCRIPT" ]; then
    # Remove carriage returns using sed
    sed -i 's/\r$//' "$HOOK_SCRIPT" 2>/dev/null || true

    # Execute the actual hook
    bash "$HOOK_SCRIPT"
else
    echo "ERROR: Hook script not found at $HOOK_SCRIPT"
    exit 1
fi
'@

    Set-Content -Path ".git/hooks/pre-commit" -Value $wrapperContent -NoNewline
}

# Verify installation
if (Test-Path ".git/hooks/pre-commit") {
    Write-Host "✅ Pre-commit hook installed successfully" -ForegroundColor Green
    Write-Host ""
    Write-Host "The following validations will run before each commit:" -ForegroundColor Yellow
    Write-Host "  • URL architecture compliance checks"
    Write-Host "  • Route validation tests"
    Write-Host "  • Razor file @page directive validation"
    Write-Host ""
    Write-Host "To bypass validation (NOT RECOMMENDED):"
    Write-Host "  git commit --no-verify"
    Write-Host ""
    Write-Host "To uninstall:"
    Write-Host "  Remove-Item .git/hooks/pre-commit"
} else {
    Write-Host "❌ ERROR: Installation failed" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Note: If you encounter line ending issues, run:" -ForegroundColor Cyan
Write-Host "  Get-Content .git-hooks/pre-commit | Set-Content -NoNewline .git/hooks/pre-commit"
