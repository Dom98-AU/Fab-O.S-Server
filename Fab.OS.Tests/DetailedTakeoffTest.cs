using Microsoft.Playwright;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Fab.OS.Tests;

[TestFixture]
public class DetailedTakeoffTest
{
    private IPlaywright _playwright;
    private IBrowser _browser;
    private IPage _page;

    [SetUp]
    public async Task Setup()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = false,  // Show browser
            SlowMo = 500
        });

        var context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
        });

        _page = await context.NewPageAsync();
    }

    [Test]
    public async Task DetailedCheckOfTakeoffCard()
    {
        Console.WriteLine("=== DETAILED TAKEOFF CARD CHECK ===\n");

        // Login first
        await _page.GotoAsync("http://localhost:5223/Account/Login");
        await _page.FillAsync("input[type='email']", "admin@steelestimation.com");
        await _page.FillAsync("input[type='password']", "Admin@123");
        await _page.ClickAsync("button:has-text('Sign In')");
        await _page.WaitForTimeoutAsync(2000);

        // Navigate to TakeoffCard
        Console.WriteLine("Navigating to http://localhost:5223/takeoffs/7");
        await _page.GotoAsync("http://localhost:5223/takeoffs/7");

        // Wait a bit for page to load
        await _page.WaitForTimeoutAsync(3000);

        // Take screenshot
        var screenshotPath = $"C:\\Temp\\takeoff-detailed-{DateTime.Now:HHmmss}.png";
        await _page.ScreenshotAsync(new() { Path = screenshotPath, FullPage = true });
        Console.WriteLine($"Screenshot saved to: {screenshotPath}\n");

        // Check for loading message
        var loadingMessage = await _page.QuerySelectorAsync("text=Loading takeoff details...");
        if (loadingMessage != null && await loadingMessage.IsVisibleAsync())
        {
            Console.WriteLine("❌ PROBLEM: 'Loading takeoff details...' message is STILL VISIBLE!");
        }
        else
        {
            Console.WriteLine("✅ No loading message visible");
        }

        // Get ALL visible text on the page
        var allText = await _page.InnerTextAsync("body");
        Console.WriteLine($"\n📄 ALL VISIBLE TEXT ON PAGE:\n{allText}\n");

        // Check for specific elements
        Console.WriteLine("🔍 CHECKING FOR ELEMENTS:");

        // Check for container
        var container = await _page.QuerySelectorAsync(".takeoff-card-container");
        Console.WriteLine($"  .takeoff-card-container exists: {container != null}");

        // Check for header
        var header = await _page.QuerySelectorAsync(".takeoff-header");
        Console.WriteLine($"  .takeoff-header exists: {header != null}");

        // Check for accordion
        var accordion = await _page.QuerySelectorAsync(".accordion");
        Console.WriteLine($"  .accordion exists: {accordion != null}");

        // Check for accordion buttons
        var accordionButtons = await _page.QuerySelectorAllAsync(".accordion-button");
        Console.WriteLine($"  Number of accordion buttons: {accordionButtons.Count}");

        // Check if any accordion section is expanded
        var expandedSection = await _page.QuerySelectorAsync(".accordion-collapse.show");
        Console.WriteLine($"  Any expanded accordion section: {expandedSection != null}");

        // Check for form controls
        var formControls = await _page.QuerySelectorAllAsync(".form-control");
        Console.WriteLine($"  Number of form controls: {formControls.Count}");

        // Check console for errors
        var consoleErrors = new List<string>();
        _page.Console += (sender, msg) =>
        {
            if (msg.Type == "error")
            {
                consoleErrors.Add(msg.Text);
            }
        };

        // Try to expand first accordion section
        if (accordionButtons.Count > 0)
        {
            Console.WriteLine("\n📂 Trying to expand first accordion section...");
            await accordionButtons[0].ClickAsync();
            await _page.WaitForTimeoutAsync(1000);

            var expandedNow = await _page.QuerySelectorAsync(".accordion-collapse.show");
            if (expandedNow != null)
            {
                var expandedContent = await expandedNow.InnerTextAsync();
                Console.WriteLine($"  Expanded content preview: {expandedContent.Substring(0, Math.Min(200, expandedContent.Length))}...");
            }
        }

        // Check page state via JavaScript
        var pageState = await _page.EvaluateAsync<string>(@"() => {
            const loading = document.querySelector('text=Loading takeoff details...');
            const container = document.querySelector('.takeoff-card-container');
            return JSON.stringify({
                hasLoadingText: loading !== null,
                hasContainer: container !== null,
                bodyHTML: document.body.innerHTML.substring(0, 500)
            });
        }");

        Console.WriteLine($"\n📊 PAGE STATE (via JS): {pageState}");

        // Keep browser open
        Console.WriteLine("\n⏰ Keeping browser open for 10 seconds for manual inspection...");
        await _page.WaitForTimeoutAsync(10000);

        Assert.Pass("Detailed check completed - see console output");
    }

    [TearDown]
    public async Task TearDown()
    {
        await _browser.CloseAsync();
        _playwright.Dispose();
    }
}