#!/bin/bash

# Fab.OS Platform - Git Hooks Installation Script
# Installs pre-commit hooks for URL architecture validation

echo "🔧 Installing Fab.OS Git Hooks"
echo "==============================="
echo ""

# Check if .git directory exists
if [ ! -d ".git" ]; then
    echo "❌ ERROR: Not a git repository"
    echo "   Run this script from the root of your git repository"
    exit 1
fi

# Create hooks directory if it doesn't exist
mkdir -p .git/hooks

# Copy pre-commit hook
echo "📋 Installing pre-commit hook..."
cp .git-hooks/pre-commit .git/hooks/pre-commit

# Make executable
chmod +x .git/hooks/pre-commit

# Verify installation
if [ -f ".git/hooks/pre-commit" ] && [ -x ".git/hooks/pre-commit" ]; then
    echo "✅ Pre-commit hook installed successfully"
    echo ""
    echo "The following validations will run before each commit:"
    echo "  • URL architecture compliance checks"
    echo "  • Route validation tests"
    echo "  • Razor file @page directive validation"
    echo ""
    echo "To bypass validation (NOT RECOMMENDED):"
    echo "  git commit --no-verify"
    echo ""
    echo "To uninstall:"
    echo "  rm .git/hooks/pre-commit"
else
    echo "❌ ERROR: Installation failed"
    exit 1
fi
