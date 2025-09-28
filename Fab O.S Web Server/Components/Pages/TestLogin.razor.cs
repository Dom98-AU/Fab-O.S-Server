using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using FabOS.WebServer.Services;
using System.Security.Claims;

namespace FabOS.WebServer.Components.Pages;

public partial class TestLogin : ComponentBase
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
    [Inject] private ILogger<TestLogin> Logger { get; set; } = default!;

    private string username = "";
    private string password = "";
    private string errorMessage = "";
    private bool isLoading = false;
    private bool showPassword = false;
    private AuthenticationState? authState;

    protected override async Task OnInitializedAsync()
    {
        authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
    }

    private async Task HandleLogin()
    {
        isLoading = true;
        errorMessage = "";

        try
        {
            // Simulate authentication logic
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                errorMessage = "Please enter username and password";
                return;
            }

            // For testing purposes - normally would call an authentication service
            if (username.ToLower() == "admin" && password == "admin")
            {
                // Create test claims
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, username),
                    new Claim(ClaimTypes.Role, "Administrator"),
                    new Claim("UserId", "1")
                };

                // Simulate successful login
                Logger.LogInformation($"User {username} logged in successfully");
                Navigation.NavigateTo("/", true);
            }
            else
            {
                errorMessage = "Invalid username or password";
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during login");
            errorMessage = "An error occurred during login. Please try again.";
        }
        finally
        {
            isLoading = false;
        }
    }

    private void TogglePasswordVisibility()
    {
        showPassword = !showPassword;
    }

    private void NavigateToRegister()
    {
        Navigation.NavigateTo("/register");
    }

    private void NavigateToForgotPassword()
    {
        Navigation.NavigateTo("/forgot-password");
    }

    private async Task HandleLogout()
    {
        try
        {
            // Simulate logout
            Logger.LogInformation("User logged out");
            Navigation.NavigateTo("/login", true);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during logout");
        }
    }

    private bool IsAuthenticated()
    {
        return authState?.User?.Identity?.IsAuthenticated ?? false;
    }

    private string GetUsername()
    {
        return authState?.User?.Identity?.Name ?? "Guest";
    }

    // Additional missing properties
    public class LoginModel
    {
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public bool RememberMe { get; set; } = false;
    }

    private LoginModel loginModel = new LoginModel();
    private bool isSuccess = false;
    private string message = "";
    private TestUser? user;

    public class TestUser
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Role { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Username { get; set; } = "";
        public bool IsActive { get; set; } = true;
        public string Company { get; set; } = "";
        public string PasswordHash { get; set; } = "";
    }

    private async Task TryPassword(string password)
    {
        loginModel.Password = password;
        await HandleLogin();
    }
}