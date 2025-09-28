using Microsoft.Playwright;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Fab.OS.Tests;

[TestFixture]
public class FirefoxSpecificTest
{
    private IPlaywright _playwright;
    private IBrowser _browser;
    private IPage _page;

    [Test]
    public async Task TestInFirefox()
    {
        Console.WriteLine("=== FIREFOX SPECIFIC TEST ===\n");

        _playwright = await Playwright.CreateAsync();

        // Launch FIREFOX specifically
        _browser = await _playwright.Firefox.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = false,  // Show browser
            SlowMo = 500
        });

        var context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
            IgnoreHTTPSErrors = true  // In case there are cert issues
        });

        _page = await context.NewPageAsync();

        // Enable console logging
        _page.Console += (sender, msg) =>
        {
            Console.WriteLine($"[Browser Console {msg.Type}]: {msg.Text}");
        };

        // Login
        await _page.GotoAsync("http://localhost:5223/Account/Login");
        await _page.FillAsync("input[type='email']", "admin@steelestimation.com");
        await _page.FillAsync("input[type='password']", "Admin@123");
        await _page.ClickAsync("button:has-text('Sign In')");
        await _page.WaitForTimeoutAsync(2000);

        // Navigate directly to TakeoffCard
        Console.WriteLine("Navigating to http://localhost:5223/takeoffs/7");
        await _page.GotoAsync("http://localhost:5223/takeoffs/7");

        // Wait for potential loading
        await _page.WaitForTimeoutAsync(5000);

        // Check what's visible
        var bodyText = await _page.InnerTextAsync("body");
        Console.WriteLine($"\n📄 PAGE CONTENT (first 500 chars):\n{bodyText.Substring(0, Math.Min(500, bodyText.Length))}\n");

        // Check for specific issues
        var loadingStuck = await _page.QuerySelectorAsync("text=Loading takeoff details...");
        if (loadingStuck != null && await loadingStuck.IsVisibleAsync())
        {
            Console.WriteLine("❌ STUCK ON LOADING MESSAGE!");
        }

        // Check if container exists
        var container = await _page.QuerySelectorAsync(".takeoff-card-container");
        Console.WriteLine($"Container exists: {container != null}");

        // Check JavaScript errors
        var jsErrors = await _page.EvaluateAsync<string>(@"() => {
            try {
                const container = document.querySelector('.takeoff-card-container');
                const loading = document.querySelector('*[textContent*=""Loading""]');
                return JSON.stringify({
                    hasContainer: container !== null,
                    containerDisplay: container ? window.getComputedStyle(container).display : 'N/A',
                    containerVisibility: container ? window.getComputedStyle(container).visibility : 'N/A',
                    bodyChildCount: document.body.children.length,
                    htmlLength: document.body.innerHTML.length
                });
            } catch (e) {
                return 'Error: ' + e.toString();
            }
        }");

        Console.WriteLine($"\nJS Check: {jsErrors}");

        // Keep browser open for manual inspection
        Console.WriteLine("\n⏰ Firefox will stay open for 15 seconds...");
        await _page.WaitForTimeoutAsync(15000);

        await _browser.CloseAsync();
        _playwright.Dispose();

        Assert.Pass("Firefox test completed");
    }
}