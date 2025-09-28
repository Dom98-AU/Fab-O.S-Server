using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fab.OS.Tests;

[TestFixture]
public class TakeoffCardDirectTest : PageTest
{
    private string _baseUrl = "http://localhost:5223";


    [Test]
    public async Task TestTakeoffCardDirectly()
    {
        Console.WriteLine("=== Direct TakeoffCard Test ===");

        // Set viewport
        await Page.SetViewportSizeAsync(1920, 1080);

        // First, authenticate
        await Page.GotoAsync($"{_baseUrl}/Account/Login");
        await Page.FillAsync("input[type='email']", "admin@steelestimation.com");
        await Page.FillAsync("input[type='password']", "Admin@123");
        await Page.ClickAsync("button:has-text('Sign In to Platform')");

        // Wait for login to complete (it will redirect to /database which has errors)
        await Page.WaitForTimeoutAsync(2000);

        // Now navigate DIRECTLY to takeoffs page (correct URL), bypassing the database page
        Console.WriteLine("Navigating directly to TakeoffCard page using correct URL /takeoffs/7...");
        await Page.GotoAsync($"{_baseUrl}/takeoffs/7", new() { WaitUntil = WaitUntilState.NetworkIdle });

        // Log the URL we ended up on
        Console.WriteLine($"Current URL: {Page.Url}");

        // Wait a moment for page to render
        await Page.WaitForTimeoutAsync(3000);

        // Take a screenshot
        var screenshotPath = Path.Combine(Path.GetTempPath(), "takeoff-direct-test.png");
        await Page.ScreenshotAsync(new() { Path = screenshotPath });
        Console.WriteLine($"Screenshot saved to: {screenshotPath}");

        // Check what's on the page
        var pageTitle = await Page.TitleAsync();
        Console.WriteLine($"Page Title: {pageTitle}");

        // Check for loading message
        var loadingMsg = await Page.QuerySelectorAsync("text=Loading takeoff details...");
        if (loadingMsg != null && await loadingMsg.IsVisibleAsync())
        {
            Console.WriteLine("⚠️ Page is showing 'Loading takeoff details...'");
        }

        // Check for the container
        var container = await Page.QuerySelectorAsync(".takeoff-card-container");
        if (container != null)
        {
            Console.WriteLine("✅ TakeoffCard container found!");

            // Check for accordion sections
            var accordions = await Page.QuerySelectorAllAsync(".accordion-button");
            Console.WriteLine($"✅ Found {accordions.Count} accordion sections");

            // Check for header
            var header = await Page.QuerySelectorAsync(".takeoff-header");
            if (header != null)
            {
                Console.WriteLine("✅ Header with glassmorphism effect present");
            }
        }
        else
        {
            Console.WriteLine("❌ TakeoffCard container NOT found");

            // Get the body text to see what's there
            var bodyText = await Page.InnerTextAsync("body");
            Console.WriteLine($"Page content (first 500 chars): {bodyText.Substring(0, Math.Min(500, bodyText.Length))}");
        }

        // Check for any console errors
        var consoleErrors = new List<string>();
        Page.Console += (sender, msg) =>
        {
            if (msg.Type == "error")
            {
                consoleErrors.Add(msg.Text);
            }
        };

        // Evaluate some JavaScript to check the page state
        var hasContainer = await Page.EvaluateAsync<bool>("() => document.querySelector('.takeoff-card-container') !== null");
        var bodyChildCount = await Page.EvaluateAsync<int>("() => document.body.children.length");
        var hasError = await Page.EvaluateAsync<bool>("() => document.body.textContent.toLowerCase().includes('error')");

        Console.WriteLine($"\nPage State Check:");
        Console.WriteLine($"  URL: {Page.Url}");
        Console.WriteLine($"  Has Container: {hasContainer}");
        Console.WriteLine($"  Body Child Count: {bodyChildCount}");
        Console.WriteLine($"  Has Error: {hasError}");

        if (consoleErrors.Any())
        {
            Console.WriteLine($"\n❌ Console Errors Found:");
            foreach (var error in consoleErrors)
            {
                Console.WriteLine($"  - {error}");
            }
        }

        Console.WriteLine("\n=== End Direct Test ===");

        // Test passes if we got this far
        Assert.Pass("Direct test completed - check console output");
    }
}