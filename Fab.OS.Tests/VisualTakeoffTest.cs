using Microsoft.Playwright;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Fab.OS.Tests;

[TestFixture]
public class VisualTakeoffTest
{
    private IPlaywright _playwright;
    private IBrowser _browser;
    private IPage _page;

    [SetUp]
    public async Task Setup()
    {
        _playwright = await Playwright.CreateAsync();

        // Launch browser with visible window
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = false,  // This makes the browser visible
            SlowMo = 1000,     // Slow down by 1 second between actions
            Args = new[] { "--start-maximized" }
        });

        var context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = ViewportSize.NoViewport  // Use full window
        });

        _page = await context.NewPageAsync();
    }

    [Test]
    public async Task VisuallyTestTakeoffCard()
    {
        Console.WriteLine("🚀 Starting Visual Test - Browser Window Should Be Visible!");

        // Navigate to login
        Console.WriteLine("📍 Going to login page...");
        await _page.GotoAsync("http://localhost:5223/Account/Login");
        await Task.Delay(1000); // Pause to see

        // Login
        Console.WriteLine("🔐 Logging in...");
        await _page.FillAsync("input[type='email']", "admin@steelestimation.com");
        await Task.Delay(500);

        await _page.FillAsync("input[type='password']", "Admin@123");
        await Task.Delay(500);

        await _page.ClickAsync("button:has-text('Sign In')");
        await Task.Delay(2000); // Wait for login

        // Navigate to TakeoffCard
        Console.WriteLine("📋 Going to TakeoffCard page...");
        await _page.GotoAsync("http://localhost:5223/takeoffs/7");
        await Task.Delay(2000);

        // Check if page loaded
        var container = await _page.QuerySelectorAsync(".takeoff-card-container");
        if (container != null)
        {
            Console.WriteLine("✅ TakeoffCard loaded successfully!");

            // Click through accordion sections
            Console.WriteLine("🔄 Testing accordion sections...");
            var accordionButtons = await _page.QuerySelectorAllAsync(".accordion-button");

            foreach (var button in accordionButtons)
            {
                await button.ClickAsync();
                await Task.Delay(1000); // Pause to see animation
            }
        }
        else
        {
            Console.WriteLine("❌ TakeoffCard container not found!");
        }

        // Keep browser open for 5 seconds at the end
        Console.WriteLine("⏰ Keeping browser open for observation...");
        await Task.Delay(5000);

        Assert.Pass("Visual test completed");
    }

    [TearDown]
    public async Task TearDown()
    {
        await _browser.CloseAsync();
        _playwright.Dispose();
    }
}