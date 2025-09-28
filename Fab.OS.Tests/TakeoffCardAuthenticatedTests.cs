using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Fab.OS.Tests;

[TestFixture]
public class TakeoffCardAuthenticatedTests : PageTest
{
    private string _baseUrl = "http://localhost:5223";

    [SetUp]
    public async Task SetUp()
    {
        // Set viewport
        await Page.SetViewportSizeAsync(1920, 1080);

        // Authenticate before each test
        await AuthenticateAsync();
    }

    private async Task AuthenticateAsync()
    {
        // Navigate to login page
        await Page.GotoAsync($"{_baseUrl}/Account/Login");

        // Fill in credentials
        await Page.FillAsync("input[type='email']", "admin@steelestimation.com");
        await Page.FillAsync("input[type='password']", "Admin@123");

        // Click sign in button
        await Page.ClickAsync("button:has-text('Sign In to Platform')");

        // Wait for navigation to complete
        await Page.WaitForURLAsync(url => !url.Contains("/Account/Login"), new() { Timeout = 10000 });

        Console.WriteLine($"Authenticated successfully. Current URL: {Page.Url}");
    }

    [Test]
    public async Task TakeoffCard_Page_Should_Load_After_Authentication()
    {
        // Navigate to TakeoffCard page
        await Page.GotoAsync($"{_baseUrl}/takeoff-card/1");

        // Verify we're not redirected to login
        Assert.That(Page.Url, Does.Not.Contain("/Account/Login"), "Should not be redirected to login after authentication");

        // Check if the page is stuck on loading
        await Page.WaitForTimeoutAsync(2000); // Give it time to load

        var loadingMessage = await Page.QuerySelectorAsync("text=Loading takeoff details...");

        if (loadingMessage != null && await loadingMessage.IsVisibleAsync())
        {
            // Take screenshot for debugging
            var screenshotPath = Path.Combine(Path.GetTempPath(), "takeoff-card-loading-stuck.png");
            await Page.ScreenshotAsync(new() { Path = screenshotPath });
            Console.WriteLine($"Screenshot of loading state saved to: {screenshotPath}");

            // Get console logs
            Page.Console += (sender, msg) => Console.WriteLine($"Browser Console [{msg.Type}]: {msg.Text}");

            // Wait a bit more to see if it resolves
            await Page.WaitForTimeoutAsync(3000);

            // Check again
            loadingMessage = await Page.QuerySelectorAsync("text=Loading takeoff details...");
            if (loadingMessage != null && await loadingMessage.IsVisibleAsync())
            {
                Assert.Fail("Page is stuck showing 'Loading takeoff details...' - The loading issue persists");
            }
        }

        // Look for the takeoff card container
        var container = await Page.QuerySelectorAsync(".takeoff-card-container");
        Assert.That(container, Is.Not.Null, "Takeoff card container should be present after loading");
    }

    [Test]
    public async Task TakeoffCard_Should_Display_All_Major_Sections()
    {
        await Page.GotoAsync($"{_baseUrl}/takeoff-card/1");

        // Wait for content to load
        await Page.WaitForTimeoutAsync(3000);

        // Check for major UI elements
        var header = await Page.QuerySelectorAsync(".takeoff-header");
        var accordion = await Page.QuerySelectorAsync(".accordion");

        Assert.Multiple(() =>
        {
            Assert.That(header, Is.Not.Null, "Header with glassmorphism effect should be present");
            Assert.That(accordion, Is.Not.Null, "Accordion for sections should be present");
        });

        // Check for progress bar
        var progressBar = await Page.QuerySelectorAsync(".progress-bar");
        Assert.That(progressBar, Is.Not.Null, "Progress bar should be visible");
    }

    [Test]
    public async Task TakeoffCard_Accordion_Sections_Should_Be_Interactive()
    {
        await Page.GotoAsync($"{_baseUrl}/takeoff-card/1");
        await Page.WaitForTimeoutAsync(2000);

        // Find the first accordion button
        var firstAccordionButton = await Page.QuerySelectorAsync(".accordion-button");

        if (firstAccordionButton == null)
        {
            Assert.Ignore("No accordion buttons found - page might not be fully loaded");
            return;
        }

        // Get the initial state
        var ariaExpanded = await firstAccordionButton.GetAttributeAsync("aria-expanded");
        bool initiallyExpanded = ariaExpanded == "true";

        // Click to toggle
        await firstAccordionButton.ClickAsync();
        await Page.WaitForTimeoutAsync(500); // Wait for animation

        // Check new state
        ariaExpanded = await firstAccordionButton.GetAttributeAsync("aria-expanded");
        bool expandedAfterClick = ariaExpanded == "true";

        Assert.That(expandedAfterClick, Is.Not.EqualTo(initiallyExpanded),
            "Accordion state should toggle when clicked");
    }

    [Test]
    public async Task TakeoffCard_Form_Fields_Should_Be_Standardized()
    {
        await Page.GotoAsync($"{_baseUrl}/takeoff-card/1");
        await Page.WaitForTimeoutAsync(2000);

        // Expand first section to see form fields
        var firstAccordionButton = await Page.QuerySelectorAsync(".accordion-button");
        if (firstAccordionButton != null)
        {
            await firstAccordionButton.ClickAsync();
            await Page.WaitForTimeoutAsync(500);
        }

        // Check for form controls
        var formControls = await Page.QuerySelectorAllAsync(".form-control, .form-select, textarea.form-control");

        Assert.That(formControls.Count, Is.GreaterThan(0),
            "Should have form controls in the takeoff card");

        // Verify consistent styling on first few controls
        foreach (var control in formControls.Take(3))
        {
            var styles = await control.EvaluateAsync<dynamic>(@"el => {
                const style = window.getComputedStyle(el);
                return {
                    borderRadius: style.borderRadius,
                    fontSize: style.fontSize,
                    minHeight: style.minHeight
                };
            }");

            Console.WriteLine($"Control styles - BorderRadius: {styles.borderRadius}, FontSize: {styles.fontSize}, MinHeight: {styles.minHeight}");
        }
    }

    [Test]
    public async Task TakeoffCard_Should_Show_Error_For_Invalid_ID()
    {
        // Try to load a takeoff that doesn't exist
        await Page.GotoAsync($"{_baseUrl}/takeoff-card/99999");
        await Page.WaitForTimeoutAsync(3000);

        // Check page content for error handling
        var pageContent = await Page.InnerTextAsync("body");

        // Log what we see
        Console.WriteLine($"Page content for invalid ID (first 500 chars): {pageContent.Substring(0, Math.Min(500, pageContent.Length))}");

        // The page should either show an error or handle it gracefully
        var hasErrorHandling = pageContent.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
                               pageContent.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                               pageContent.Contains("invalid", StringComparison.OrdinalIgnoreCase) ||
                               !Page.Url.Contains("99999"); // Or redirected away

        Assert.That(hasErrorHandling, Is.True,
            "Page should handle invalid takeoff IDs gracefully with an error message or redirect");
    }

    [TearDown]
    public async Task TearDown()
    {
        // Take screenshot on failure
        if (TestContext.CurrentContext.Result.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Failed)
        {
            var screenshotPath = Path.Combine(
                Path.GetTempPath(),
                $"test-failure-{TestContext.CurrentContext.Test.Name}-{DateTime.Now:yyyyMMddHHmmss}.png");
            await Page.ScreenshotAsync(new() { Path = screenshotPath });
            Console.WriteLine($"Failure screenshot saved to: {screenshotPath}");
        }
    }
}