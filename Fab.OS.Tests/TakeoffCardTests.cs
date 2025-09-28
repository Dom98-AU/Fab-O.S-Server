using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using System.Text.RegularExpressions;

namespace Fab.OS.Tests;

[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class TakeoffCardTests : PageTest
{
    private string _baseUrl = "http://localhost:5223";

    [SetUp]
    public async Task SetUp()
    {
        // Set up browser context with longer timeout for development
        Page.SetDefaultTimeout(30000);
        await Page.SetViewportSizeAsync(1920, 1080);
    }

    [Test]
    public async Task TakeoffCard_Should_Load_Successfully()
    {
        // First authenticate if needed
        await AuthenticateIfRequired();

        // Navigate to the TakeoffCard page with a test ID
        // Assuming ID 1 exists in the database for testing
        await Page.GotoAsync($"{_baseUrl}/takeoff-card/1");

        // Wait for the page to load and check that loading message disappears
        // Check if we're redirected to login
        var url = Page.Url;
        if (url.Contains("login", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Ignore("Authentication is required - skipping test");
            return;
        }

        // Wait for the container or check if the page loaded correctly
        try
        {
            await Page.WaitForSelectorAsync(".takeoff-card-container", new() { State = WaitForSelectorState.Visible, Timeout = 5000 });
        }
        catch (TimeoutException)
        {
            // Check if we're stuck on loading
            var loadingElement = await Page.QuerySelectorAsync("text=Loading takeoff details...");
            if (loadingElement != null)
            {
                Assert.Fail("Page is stuck on 'Loading takeoff details...' message");
            }
            else
            {
                Assert.Fail("Takeoff card container did not appear within timeout");
            }
        }

        // Verify that the loading message is not visible
        var loadingMsg = await Page.QuerySelectorAsync("text=Loading takeoff details...");
        Assert.That(loadingMsg, Is.Null, "Loading message should not be visible after page loads");

        // Verify the main container is present
        var container = await Page.QuerySelectorAsync(".takeoff-card-container");
        Assert.That(container, Is.Not.Null, "Takeoff card container should be present");
    }

    private async Task AuthenticateIfRequired()
    {
        // Try to navigate to the home page
        await Page.GotoAsync(_baseUrl);

        // Check if we're on the login page
        if (Page.Url.Contains("login", StringComparison.OrdinalIgnoreCase))
        {
            // Look for login form and try to authenticate with test credentials
            var usernameField = await Page.QuerySelectorAsync("input[type='email'], input[name*='username'], input[name*='email']");
            var passwordField = await Page.QuerySelectorAsync("input[type='password']");

            if (usernameField != null && passwordField != null)
            {
                // Use test credentials (you may need to adjust these)
                await usernameField.FillAsync("test@fabos.com");
                await passwordField.FillAsync("TestPassword123!");

                // Find and click the login button
                var loginButton = await Page.QuerySelectorAsync("button[type='submit'], input[type='submit'], button:has-text('Login'), button:has-text('Sign in')");
                if (loginButton != null)
                {
                    await loginButton.ClickAsync();
                    // Wait for navigation after login
                    await Page.WaitForTimeoutAsync(2000);
                }
            }
        }
    }

    [Test]
    public async Task TakeoffCard_Should_Display_Header_With_Progress()
    {
        await Page.GotoAsync($"{_baseUrl}/takeoff-card/1");
        await Page.WaitForSelectorAsync(".takeoff-header", new() { State = WaitForSelectorState.Visible });

        // Check for glassmorphism header
        var header = await Page.QuerySelectorAsync(".takeoff-header");
        Assert.That(header, Is.Not.Null, "Header should be present");

        // Check for progress indicator
        var progressBar = await Page.QuerySelectorAsync(".progress-bar");
        Assert.That(progressBar, Is.Not.Null, "Progress bar should be present");

        // Check for progress percentage text
        var progressText = await Page.TextContentAsync(".progress-info");
        Assert.That(progressText, Does.Contain("%"), "Progress percentage should be displayed");
    }

    [Test]
    public async Task TakeoffCard_Should_Have_All_Accordion_Sections()
    {
        await Page.GotoAsync($"{_baseUrl}/takeoff-card/1");
        await Page.WaitForSelectorAsync(".accordion", new() { State = WaitForSelectorState.Visible });

        // List of expected accordion sections
        string[] expectedSections =
        {
            "Project Information",
            "Customer Details",
            "Site Location",
            "Trade Information",
            "Drawings and Documents",
            "Items and Quantities",
            "Costs and Budget",
            "Notes and Communication",
            "Approval and Sign-off"
        };

        foreach (var section in expectedSections)
        {
            var sectionButton = await Page.QuerySelectorAsync($"button:has-text('{section}')");
            Assert.That(sectionButton, Is.Not.Null, $"Section '{section}' should be present");
        }
    }

    [Test]
    public async Task TakeoffCard_Accordion_Should_Expand_And_Collapse()
    {
        await Page.GotoAsync($"{_baseUrl}/takeoff-card/1");
        await Page.WaitForSelectorAsync(".accordion", new() { State = WaitForSelectorState.Visible });

        // Find the first accordion button (Project Information)
        var accordionButton = await Page.QuerySelectorAsync(".accordion-button");
        Assert.That(accordionButton, Is.Not.Null, "Accordion button should exist");

        // Get the associated content panel
        var contentPanel = await Page.QuerySelectorAsync("#collapseOne");

        // Check if initially expanded (based on your implementation)
        var isExpanded = contentPanel != null && await contentPanel.IsVisibleAsync();

        // Click to toggle
        await accordionButton.ClickAsync();
        await Page.WaitForTimeoutAsync(500); // Wait for animation

        // Check the state changed
        var isExpandedAfterClick = contentPanel != null && await contentPanel.IsVisibleAsync();
        Assert.That(isExpandedAfterClick, Is.Not.EqualTo(isExpanded), "Accordion state should toggle on click");
    }

    [Test]
    public async Task TakeoffCard_Form_Fields_Should_Have_Consistent_Styling()
    {
        await Page.GotoAsync($"{_baseUrl}/takeoff-card/1");
        await Page.WaitForSelectorAsync(".accordion", new() { State = WaitForSelectorState.Visible });

        // Expand the first section to see form fields
        await Page.ClickAsync(".accordion-button:first-child");
        await Page.WaitForTimeoutAsync(500);

        // Get all form controls
        var formControls = await Page.QuerySelectorAllAsync(".form-control");
        Assert.That(formControls.Count, Is.GreaterThan(0), "Form controls should be present");

        // Check that all form controls have consistent styling
        foreach (var control in formControls)
        {
            var borderRadius = await control.EvaluateAsync<string>("el => window.getComputedStyle(el).borderRadius");
            var padding = await control.EvaluateAsync<string>("el => window.getComputedStyle(el).padding");
            var fontSize = await control.EvaluateAsync<string>("el => window.getComputedStyle(el).fontSize");

            Assert.That(borderRadius, Is.EqualTo("8px"), "Border radius should be 8px");
            Assert.That(fontSize, Is.EqualTo("16px"), "Font size should be 16px (1rem)");
        }
    }

    [Test]
    public async Task TakeoffCard_Should_Display_Action_Buttons()
    {
        await Page.GotoAsync($"{_baseUrl}/takeoff-card/1");
        await Page.WaitForSelectorAsync(".takeoff-card-container", new() { State = WaitForSelectorState.Visible });

        // Scroll to bottom to see action buttons
        await Page.EvaluateAsync("window.scrollTo(0, document.body.scrollHeight)");
        await Page.WaitForTimeoutAsync(500);

        // Check for action buttons
        var saveButton = await Page.QuerySelectorAsync("button:has-text('Save')");
        var cancelButton = await Page.QuerySelectorAsync("button:has-text('Cancel')");

        Assert.That(saveButton, Is.Not.Null, "Save button should be present");
        Assert.That(cancelButton, Is.Not.Null, "Cancel button should be present");
    }

    [Test]
    public async Task TakeoffCard_Should_Handle_Invalid_ID_Gracefully()
    {
        // Navigate to a page with an invalid ID
        await Page.GotoAsync($"{_baseUrl}/takeoff-card/99999");

        // Should show an error message or redirect
        await Page.WaitForTimeoutAsync(2000);

        // Check if error message is displayed or page handles it gracefully
        var pageContent = await Page.ContentAsync();
        var hasError = pageContent.Contains("not found") ||
                       pageContent.Contains("error") ||
                       pageContent.Contains("Error");

        // Either an error message should be shown or it should redirect
        var currentUrl = Page.Url;
        Assert.That(hasError || !currentUrl.Contains("99999"),
            Is.True,
            "Page should handle invalid ID with error message or redirect");
    }

    [Test]
    public async Task TakeoffCard_Animations_Should_Be_Present()
    {
        await Page.GotoAsync($"{_baseUrl}/takeoff-card/1");
        await Page.WaitForSelectorAsync(".takeoff-card-container", new() { State = WaitForSelectorState.Visible });

        // Check for fade-in animation on container
        var container = await Page.QuerySelectorAsync(".takeoff-card-container");
        Assert.That(container, Is.Not.Null, "Container should exist");
        var animation = await container!.EvaluateAsync<string>("el => window.getComputedStyle(el).animation");

        Assert.That(animation, Does.Contain("fadeIn"), "Container should have fade-in animation");
    }

    [Test]
    public async Task TakeoffCard_Should_Have_Responsive_Design()
    {
        // Test mobile viewport
        await Page.SetViewportSizeAsync(375, 667);
        await Page.GotoAsync($"{_baseUrl}/takeoff-card/1");
        await Page.WaitForSelectorAsync(".takeoff-card-container", new() { State = WaitForSelectorState.Visible });

        var container = await Page.QuerySelectorAsync(".takeoff-card-container");
        Assert.That(container, Is.Not.Null, "Container should be present on mobile");

        // Test tablet viewport
        await Page.SetViewportSizeAsync(768, 1024);
        await Page.ReloadAsync();
        await Page.WaitForSelectorAsync(".takeoff-card-container", new() { State = WaitForSelectorState.Visible });

        container = await Page.QuerySelectorAsync(".takeoff-card-container");
        Assert.That(container, Is.Not.Null, "Container should be present on tablet");

        // Test desktop viewport
        await Page.SetViewportSizeAsync(1920, 1080);
        await Page.ReloadAsync();
        await Page.WaitForSelectorAsync(".takeoff-card-container", new() { State = WaitForSelectorState.Visible });

        container = await Page.QuerySelectorAsync(".takeoff-card-container");
        Assert.That(container, Is.Not.Null, "Container should be present on desktop");
    }

    [Test]
    public async Task TakeoffCard_Glassmorphism_Effects_Should_Be_Applied()
    {
        await Page.GotoAsync($"{_baseUrl}/takeoff-card/1");
        await Page.WaitForSelectorAsync(".takeoff-header", new() { State = WaitForSelectorState.Visible });

        // Check for glassmorphism CSS properties
        var header = await Page.QuerySelectorAsync(".takeoff-header");
        Assert.That(header, Is.Not.Null, "Header should exist");
        var backdropFilter = await header!.EvaluateAsync<string>("el => window.getComputedStyle(el).backdropFilter");
        var background = await header!.EvaluateAsync<string>("el => window.getComputedStyle(el).background");

        Assert.That(backdropFilter, Does.Contain("blur"), "Header should have backdrop blur effect");
        Assert.That(background, Does.Contain("rgba") | Does.Contain("linear-gradient"),
            "Header should have semi-transparent background or gradient");
    }
}