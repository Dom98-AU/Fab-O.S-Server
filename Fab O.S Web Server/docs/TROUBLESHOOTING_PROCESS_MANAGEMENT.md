# Troubleshooting Stubborn Dotnet Processes in WSL

## Problem

When running `dotnet run` or rebuilding the Fab.OS Web Server, you may encounter port binding errors like:

```
Failed to bind to address http://0.0.0.0:5224: address already in use
```

This happens when dotnet processes from previous sessions don't respond to standard kill commands like `killall -9 dotnet`.

## Quick Solution

### Step 1: Find the Process Using the Port

Use `lsof` to find which process is using the port:

```bash
lsof -i :5224
```

**Example Output:**
```
COMMAND     PID       USER   FD   TYPE DEVICE SIZE/OFF NODE NAME
FabOS.Web 52286 dmoraitis   218u  IPv4 123456      0t0  TCP *:5224 (LISTEN)
```

The important information here:
- **COMMAND**: `FabOS.Web` - The compiled binary name
- **PID**: `52286` - The process ID we need to kill

### Step 2: Kill the Specific Process

Use the PID from Step 1:

```bash
kill -9 52286
```

Replace `52286` with the actual PID from your `lsof` output.

### Step 3: Verify the Port is Free

```bash
lsof -i :5224
```

This should return no results if successful.

## Alternative Diagnostic Commands

If `lsof` is not available, you can use these alternatives:

### Using netstat
```bash
netstat -tulpn | grep 5224
```

### Using ss (faster than netstat)
```bash
ss -tulpn | grep 5224
```

## Why `killall` Doesn't Always Work

When you compile a .NET application, the running process name changes from `dotnet` to the compiled binary name (e.g., `FabOS.Web`, `FabOS.WebServer`). This means:

- `killall -9 dotnet` ❌ Won't kill compiled binaries
- `kill -9 <PID>` ✅ Always works when you have the correct PID

## Complete Cleanup Script

To kill ALL dotnet-related processes (both in WSL and Windows):

```bash
# Kill WSL dotnet processes
killall -9 dotnet 2>/dev/null

# Find and kill any process on port 5224
PID=$(lsof -ti :5224)
if [ ! -z "$PID" ]; then
    kill -9 $PID
    echo "Killed process on port 5224 (PID: $PID)"
fi

# Kill Windows dotnet processes (if running from WSL)
taskkill.exe //F //IM dotnet.exe 2>/dev/null

# Verify
echo "Checking if port 5224 is free:"
lsof -i :5224
```

## Port Reference

Common Fab.OS ports:
- **5224**: Fab O.S Web Server (development)
- **5223**: Alternative development port
- **5000-5001**: Default Kestrel ports

## Additional Tips

1. **Check all running dotnet processes:**
   ```bash
   ps aux | grep dotnet
   ```

2. **Kill multiple processes on different ports:**
   ```bash
   for port in 5224 5223 5000 5001; do
       PID=$(lsof -ti :$port)
       [ ! -z "$PID" ] && kill -9 $PID && echo "Killed port $port (PID: $PID)"
   done
   ```

3. **Check if any FabOS processes are running:**
   ```bash
   ps aux | grep -i fabos
   ```

## When to Use This Guide

Use this troubleshooting process when:
- You see "address already in use" errors
- The browser still shows old code after rebuild
- `killall` commands don't stop the server
- You need to ensure a clean slate before debugging

## References

- Created: 2025-10-18
- Last Updated: 2025-10-18
- Related: Nutrient POC development, PSPDFKit testing
