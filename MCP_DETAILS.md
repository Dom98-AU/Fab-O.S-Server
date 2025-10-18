# MCP Server Configuration Details

**Last Updated:** 2025-10-17
**System:** WSL2 (Ubuntu on Windows)
**Claude Code Version:** Global installation with auto-updates enabled

---

## Overview

This document provides a comprehensive overview of all Model Context Protocol (MCP) servers installed and configured for the Fab.OS Platform project.

### Installed MCP Servers

| Server Name | Status | Type | Purpose |
|-------------|--------|------|---------|
| **fabos-context** | ✓ Connected | Custom | Project context and knowledge management |
| **fabos-mssql** | ✓ Connected | Custom | SQL Server database operations |
| **playwright** | ✓ Connected | Official | Browser automation and testing |

---

## 1. Playwright MCP Server

### Installation Status
✓ **Fully Functional and Connected**

### Configuration

```json
{
  "playwright": {
    "type": "stdio",
    "command": "npx",
    "args": ["@playwright/mcp@latest"],
    "env": {},
    "status": "connected"
  }
}
```

**Installation Command:** `claude mcp add playwright npx @playwright/mcp@latest`

### Version Information

- **Playwright CLI:** 1.56.0
- **MCP Package:** @playwright/mcp@latest
- **Installation Method:** npx (on-demand execution)
- **Execution Environment:** WSL2

### Node.js Environment

**WSL Environment (Active):**
- Node.js: v22.16.0
- npm: 10.9.2
- npx: /home/dmoraitis/.nvm/versions/node/v22.16.0/bin/npx

### Installed Browsers

Located in: `/home/dmoraitis/.cache/ms-playwright` (WSL)

| Browser | Version | Type | Status |
|---------|---------|------|--------|
| Chromium | 1187 | Current | ✓ Active |
| Chromium Headless Shell | 1187 | Current | ✓ Active |
| Firefox | 1490 | Current | ✓ Active |
| WebKit | 2203 | Current | ✓ Active |

**Storage Usage:** ~1-2 GB for all browsers

### Available Tools

The Playwright MCP server provides the following capabilities:

#### Navigation & Page Control
- `mcp__playwright__browser_navigate` - Navigate to URLs
- `mcp__playwright__browser_navigate_back` - Go back to previous page
- `mcp__playwright__browser_close` - Close the browser page
- `mcp__playwright__browser_resize` - Resize browser window
- `mcp__playwright__browser_tabs` - Manage browser tabs (list, create, close, select)

#### Page Inspection
- `mcp__playwright__browser_snapshot` - Capture accessibility snapshot (recommended)
- `mcp__playwright__browser_take_screenshot` - Take visual screenshots (PNG/JPEG)
- `mcp__playwright__browser_console_messages` - Retrieve console logs
- `mcp__playwright__browser_network_requests` - Monitor network activity

#### User Interactions
- `mcp__playwright__browser_click` - Click elements (left, right, middle, double-click)
- `mcp__playwright__browser_type` - Type text into editable elements
- `mcp__playwright__browser_hover` - Hover over elements
- `mcp__playwright__browser_drag` - Drag and drop between elements
- `mcp__playwright__browser_press_key` - Press keyboard keys

#### Form Handling
- `mcp__playwright__browser_fill_form` - Fill multiple form fields at once
- `mcp__playwright__browser_select_option` - Select dropdown options
- `mcp__playwright__browser_file_upload` - Upload files

#### Advanced Features
- `mcp__playwright__browser_evaluate` - Execute JavaScript on page or element
- `mcp__playwright__browser_handle_dialog` - Handle alerts/prompts/confirms
- `mcp__playwright__browser_wait_for` - Wait for text appearance/disappearance or time
- `mcp__playwright__browser_install` - Install missing browsers

### Command-Line Options

The Playwright MCP server supports the following options:

```bash
--allowed-hosts <hosts...>       # Restrict server to specific hosts
--allowed-origins <origins>      # Semicolon-separated allowed origins
--blocked-origins <origins>      # Semicolon-separated blocked origins
--block-service-workers          # Prevent service worker registration
--browser <browser>              # Choose browser (chrome, firefox, webkit)
```

### Maintenance Recommendations

1. **Regular Updates:** Run `npx playwright install` to update browsers
2. **Cleanup Old Versions:** Remove legacy browser versions (chromium-1181) to save disk space
3. **Monitor Storage:** Playwright browsers consume 2-3 GB
4. **Version Pinning:** Consider pinning `@playwright/mcp` to specific version for stability

---

## 2. FabOS Context MCP Server

### Installation Status
✓ **Connected**

### Configuration

```json
{
  "fabos-context": {
    "command": "node /mnt/c/Fab.OS-MCP/servers/fabos-context/index.js",
    "status": "connected"
  }
}
```

### Purpose

Custom MCP server for managing project-specific context, knowledge base, and development tracking for the Fab.OS Platform.

### Available Tools

#### Context Management
- `mcp__fabos-context__get_project_context` - Retrieve current project structure and context
- `mcp__fabos-context__update_context` - Update project context documentation sections
- `mcp__fabos-context__search_knowledge` - Search the knowledge base

#### Development Tracking
- `mcp__fabos-context__get_recent_changes` - Get recent development log entries
- `mcp__fabos-context__log_change` - Record changes to the development log
- `mcp__fabos-context__track_database_change` - Track database schema/migration changes

#### Architecture Patterns
- `mcp__fabos-context__get_architecture_patterns` - Retrieve web architecture patterns and guidelines
  - Supports category filtering: page-types, components, visual, etc.

#### Terminal Coordination
- `mcp__fabos-context__claim_module` - Claim ownership of modules/files for multi-terminal coordination
- `mcp__fabos-context__check_ownership` - Check if a file is claimed by another terminal
- `mcp__fabos-context__release_module` - Release claimed modules/files

### Server Location

**Path:** `/mnt/c/Fab.OS-MCP/servers/fabos-context/`

---

## 3. FabOS MSSQL MCP Server

### Installation Status
✓ **Connected**

### Configuration

```json
{
  "fabos-mssql": {
    "command": "node /mnt/c/Fab.OS-MCP/servers/mssql-nodejs/index.js",
    "status": "connected"
  }
}
```

### Purpose

Custom MCP server providing direct access to Microsoft SQL Server database operations for the Fab.OS Platform.

### Available Tools

#### Database Schema
- `mcp__fabos-mssql__get_database_schema` - Retrieve complete database schema information
- `mcp__fabos-mssql__get_table_statistics` - Get table row counts and statistics

#### Query Execution
- `mcp__fabos-mssql__query_database` - Execute SQL queries against the database
- `mcp__fabos-mssql__execute_sqlcmd` - Execute sqlcmd commands directly

### Server Location

**Path:** `/mnt/c/Fab.OS-MCP/servers/mssql-nodejs/`

### SQL Server Configuration

**Tools Available:**
- `/opt/mssql-tools/bin/sqlcmd` - SQL Server 2017 command-line tools
- `/opt/mssql-tools18/bin/sqlcmd` - SQL Server 2018+ command-line tools

**Connection Methods:**
- Direct sqlcmd execution via Bash
- Node.js-based query interface via MCP
- Entity Framework migrations via `dotnet ef`

---

## System Configuration

### Claude Code Settings

**Configuration File:** `~/.claude.json`

**Key Settings:**
- Install Method: Global
- Auto Updates: Enabled
- Theme: dark-ansi
- Number of Startups: 116
- Show Expanded Todos: true

### Environment Details

**Operating System:**
- Platform: Linux (WSL2)
- Kernel: 6.6.87.2-microsoft-standard-WSL2
- Distribution: Ubuntu on Windows

**Project Location:**
- Working Directory: `/mnt/c/Fab.OS Platform/Fab O.S Server`
- Git Repository: Yes (main branch)

**.NET Environment (WSL):**
- SDK Version: 8.0.413
- Installation Path: `~/.dotnet`
- PATH Configuration: `export PATH="$HOME/.dotnet:$PATH"`
- Runtime: Compatible with .NET 8.0.415 projects

**Application Status:**
- Running: Yes
- Port: 5224
- URL: http://localhost:5224
- Environment: Development
- Status: ✓ Responding (HTTP 302)

### Approved Tool Permissions

The following tools have been pre-approved for use without additional permission prompts:

#### File System Access
- `Read(//mnt/c/Fab.OS Platform/**)`
- `Read(//mnt/c/**)`
- `Read(//home/dmoraitis/**)`
- `Read(//home/dmoraitis/.docker/**)`

#### Build & Development
- `Bash(cmd.exe /c "dotnet build")`
- `Bash(dotnet build:*)`
- `Bash(dotnet run:*)`
- `Bash(dotnet clean:*)`
- `Bash(dotnet ef database:*)`
- `Bash(dotnet ef migrations:*)`

#### Database Operations
- `Bash(sqlcmd:*)`
- `Bash(/opt/mssql-tools/bin/sqlcmd:*)`
- `Bash(/opt/mssql-tools18/bin/sqlcmd:*)`
- `mcp__fabos-mssql__get_database_schema`
- `mcp__fabos-mssql__query_database`
- `mcp__fabos-mssql__get_table_statistics`
- `mcp__fabos-mssql__execute_sqlcmd`
- `mcp__fabos-context__track_database_change`

#### Git Operations
- `Bash(git worktree:*)`
- `Bash(git commit:*)`
- `Bash(git add:*)`
- `Bash(git push:*)`
- `Bash(git merge:*)`
- `Bash(git clone:*)`

#### Process Management
- `Bash(tasklist:*)`
- `Bash(taskkill:*)`
- `Bash(pkill:*)`

#### Docker
- `Bash(docker:*)`

#### MCP Context Operations
- `mcp__fabos-context__get_project_context`
- `mcp__fabos-context__get_architecture_patterns`

#### Web Fetching
- `WebFetch(domain:learnacc.autodesk.com)`
- `WebFetch(domain:www.nutrient.io)`
- `WebFetch(domain:pspdfkit.com)`
- `WebFetch(domain:www.austubemills.com.au)`
- `WebFetch(domain:github.com)`
- `WebFetch(domain:cloudapi.foxit.com)`
- `WebFetch(domain:developers.foxit.com)`

#### Playwright Browser Automation
- `mcp__playwright__browser_navigate`
- `mcp__playwright__browser_close`
- `mcp__playwright__browser_install`

#### Miscellaneous
- `Bash(cd:*)`
- `Bash(ls:*)`
- `Bash(find:*)`
- `Bash(grep:*)`
- `Bash(sed:*)`
- `Bash(awk:*)`
- `Bash(cat:*)`
- `Bash(curl:*)`
- `Bash(npm install:*)`
- `Bash(claude mcp list:*)`
- `Bash(powershell.exe:*)`

---

## Health Check Summary

| Component | Status | Last Verified |
|-----------|--------|---------------|
| fabos-context MCP | ✓ Connected | 2025-10-17 |
| fabos-mssql MCP | ✓ Connected | 2025-10-17 |
| playwright MCP | ✓ Connected | 2025-10-17 |
| Node.js v22.16.0 | ✓ Installed | 2025-10-17 |
| Playwright Browsers | ✓ Installed | 2025-10-17 |
| SQL Server Tools | ✓ Available | 2025-10-17 |

---

## Troubleshooting

### Playwright MCP Issues

**Problem:** Browser not installed
**Solution:** Run `mcp__playwright__browser_install` or `npx playwright install`

**Problem:** "ERR_INVALID_FILE_URL_PATH" error when navigating
**Known Issue:** This is a known error with Playwright MCP that masks underlying issues. It does not necessarily indicate the setup is incorrect.
**Verification:** Use `curl http://localhost:PORT` to verify the application is actually responding
**Status:** Reported issue with Playwright MCP server, not a configuration problem

**Problem:** Connection timeout
**Solution:** Check that Node.js is accessible from WSL (npx command works)

### FabOS Context/MSSQL Issues

**Problem:** Server not connecting
**Solution:** Verify Node.js can execute the server files at `/mnt/c/Fab.OS-MCP/servers/`

**Problem:** Database connection fails
**Solution:** Verify SQL Server is running and sqlcmd tools are installed

### General MCP Issues

**Problem:** MCP server not listed
**Solution:** Run `claude mcp list` to verify configuration

**Problem:** Tools not available
**Solution:** Restart Claude Code or check server health status

---

## Maintenance Tasks

### Weekly
- Review MCP server health (`claude mcp list`)
- Check for Playwright browser updates

### Monthly
- Update Playwright browsers (`npx playwright install`)
- Clean up old browser versions
- Review and update approved tool permissions

### As Needed
- Update custom MCP servers when features are added
- Review and prune development logs in fabos-context
- Optimize database queries in fabos-mssql

---

## Additional Resources

### Playwright MCP Documentation
- GitHub: https://github.com/microsoft/playwright
- MCP Integration: https://github.com/microsoft/playwright-mcp

### Custom MCP Servers
- Source: `/mnt/c/Fab.OS-MCP/servers/`
- fabos-context: Project-specific context management
- fabos-mssql: SQL Server database operations

### Claude Code Documentation
- Official docs: https://docs.claude.com/en/docs/claude-code
- MCP Protocol: https://modelcontextprotocol.io

---

**End of Document**
