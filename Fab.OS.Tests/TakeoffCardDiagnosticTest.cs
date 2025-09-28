using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using System;

namespace Fab.OS.Tests;

[TestFixture]
public class TakeoffCardDiagnosticTest : PageTest
{
    private string _baseUrl = "http://localhost:5223";

    [Test]
    public async Task DiagnoseTakeoffCardPage()
    {
        Console.WriteLine("=== TakeoffCard Page Diagnostic Test ===");

        // Set viewport
        await Page.SetViewportSizeAsync(1920, 1080);

        // Navigate to TakeoffCard page
        Console.WriteLine($"Navigating to: {_baseUrl}/takeoff-card/1");
        await Page.GotoAsync($"{_baseUrl}/takeoff-card/1");

        // Log final URL after any redirects
        Console.WriteLine($"Final URL: {Page.Url}");

        // Take a screenshot for debugging
        var screenshotPath = Path.Combine(Path.GetTempPath(), "takeoff-card-test.png");
        await Page.ScreenshotAsync(new() { Path = screenshotPath });
        Console.WriteLine($"Screenshot saved to: {screenshotPath}");

        // Get page title
        var title = await Page.TitleAsync();
        Console.WriteLine($"Page Title: {title}");

        // Check for common authentication/login indicators
        var loginIndicators = new[]
        {
            "login",
            "sign in",
            "authenticate",
            "username",
            "password"
        };

        var pageContent = await Page.ContentAsync();
        foreach (var indicator in loginIndicators)
        {
            if (pageContent.Contains(indicator, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"Found login indicator: '{indicator}'");
            }
        }

        // Check for specific elements
        Console.WriteLine("\n--- Element Check ---");

        // Check for loading message
        var loadingMsg = await Page.QuerySelectorAsync("text=Loading takeoff details...");
        Console.WriteLine($"Loading message present: {loadingMsg != null}");

        // Check for takeoff container
        var container = await Page.QuerySelectorAsync(".takeoff-card-container");
        Console.WriteLine($"Takeoff container present: {container != null}");

        // Check for any error messages
        var errorElements = await Page.QuerySelectorAllAsync("[class*='error'], [class*='alert']");
        Console.WriteLine($"Error/Alert elements found: {errorElements.Count}");

        // Get all visible text on the page (first 500 chars)
        var visibleText = await Page.InnerTextAsync("body");
        var truncatedText = visibleText.Length > 500 ? visibleText.Substring(0, 500) + "..." : visibleText;
        Console.WriteLine($"\nVisible text (first 500 chars):\n{truncatedText}");

        // Check console errors
        Page.Console += (sender, msg) =>
        {
            if (msg.Type == "error")
            {
                Console.WriteLine($"Console Error: {msg.Text}");
            }
        };

        // Wait a bit to capture any delayed errors
        await Page.WaitForTimeoutAsync(2000);

        Console.WriteLine("\n=== End Diagnostic Test ===");

        // The test passes if we got this far
        Assert.Pass("Diagnostic completed - check console output for details");
    }
}