# Git Hooks Installation Guide

## For Windows Users (Recommended)

### Option 1: Manual Installation (Simplest)
1. Open PowerShell or Command Prompt
2. Navigate to your repository:
   ```powershell
   cd "C:\Fab.OS Platform\Fab O.S Server\Fab O.S Web Server"
   ```
3. Copy the hook manually:
   ```powershell
   # Initialize git if not already done
   git init

   # Create hooks directory
   New-Item -ItemType Directory -Force -Path ".git/hooks"

   # Copy the pre-commit hook
   Copy-Item ".git-hooks/pre-commit" ".git/hooks/pre-commit" -Force
   ```
4. Done! The hook will run automatically on commits.

### Option 2: Using Git Bash
If you have Git Bash installed:
```bash
cd "/mnt/c/Fab.OS Platform/Fab O.S Server/Fab O.S Web Server"
bash .git-hooks/install-hooks.sh
```

### Option 3: Using Batch File
```cmd
cd "C:\Fab.OS Platform\Fab O.S Server\Fab O.S Web Server"
.git-hooks\install-hooks.bat
```

---

## For Linux/Mac Users

```bash
cd "/path/to/Fab O.S Server/Fab O.S Web Server"
bash .git-hooks/install-hooks.sh
```

---

## What the Pre-Commit Hook Does

When installed, this hook automatically runs **before every commit** and:

✅ Validates all changed Razor files for URL compliance
✅ Checks routes start with `/{tenantSlug}`
✅ Verifies lowercase, plural, kebab-case naming
✅ Ensures IDs have `:int` constraints
✅ Runs route validation tests
✅ **Blocks commits** if violations are found

---

## Testing the Installation

1. Make a change to any Razor file
2. Try to commit:
   ```bash
   git add .
   git commit -m "Test commit"
   ```
3. The hook should run and validate your changes

---

## Bypassing the Hook (Not Recommended)

If you need to bypass validation temporarily:
```bash
git commit --no-verify -m "Your message"
```

**⚠️ WARNING:** Only bypass if you absolutely must. Violations should be fixed before committing.

---

## Uninstalling

### Windows PowerShell:
```powershell
Remove-Item ".git/hooks/pre-commit"
```

### Git Bash / Linux / Mac:
```bash
rm .git/hooks/pre-commit
```

---

## Troubleshooting

### Issue: "command not found" errors
**Solution:** The hook requires Git Bash on Windows. Make sure Git for Windows is installed.

### Issue: Line ending errors (`\r`)
**Solution:** Run in PowerShell:
```powershell
(Get-Content .git-hooks/pre-commit) | Set-Content -NoNewline .git/hooks/pre-commit
```

### Issue: Hook doesn't run
**Solution:** Verify it was copied correctly:
```bash
ls -la .git/hooks/pre-commit
```

### Issue: Permission denied
**Solution:** Make the hook executable (Linux/Mac):
```bash
chmod +x .git/hooks/pre-commit
```

---

## Need Help?

- Check [URL-ROUTING-STANDARDS.md](../../Fab%20O.S%20System%20Architecture/URL-ROUTING-STANDARDS.md)
- Review [MODULE-DEVELOPMENT-GUIDE.md](../../Fab%20O.S%20System%20Architecture/MODULE-DEVELOPMENT-GUIDE.md)
- Contact: #fabos-architecture (Slack)
