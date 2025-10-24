@echo off
REM Fab.OS Platform - Git Hooks Installation Script (Windows Batch)
REM Installs pre-commit hooks for URL architecture validation

echo.
echo Installing Fab.OS Git Hooks
echo ===============================
echo.

REM Check if .git directory exists
if not exist ".git" (
    echo ERROR: Not a git repository
    echo        Run this script from the root of your git repository
    exit /b 1
)

REM Create hooks directory if it doesn't exist
if not exist ".git\hooks" mkdir ".git\hooks"

REM Copy pre-commit hook
echo Installing pre-commit hook...
copy /Y ".git-hooks\pre-commit" ".git\hooks\pre-commit" >nul

REM Verify installation
if exist ".git\hooks\pre-commit" (
    echo.
    echo Pre-commit hook installed successfully
    echo.
    echo The following validations will run before each commit:
    echo   - URL architecture compliance checks
    echo   - Route validation tests
    echo   - Razor file @page directive validation
    echo.
    echo To bypass validation (NOT RECOMMENDED):
    echo   git commit --no-verify
    echo.
    echo To uninstall:
    echo   del .git\hooks\pre-commit
    echo.
) else (
    echo ERROR: Installation failed
    exit /b 1
)

echo Note: Git hooks on Windows require Git Bash
echo       The hook will run automatically when you commit
echo.
pause
