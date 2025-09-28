using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Diagnostics;

namespace FabOS.WebServer.Components.Pages;

public partial class Error : ComponentBase
{
    [CascadingParameter] private HttpContext HttpContext { get; set; } = default!;

    private string? RequestId { get; set; }
    private bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    protected override void OnInitialized()
    {
        if (HttpContext != null)
        {
            var exceptionHandlerFeature = HttpContext.Features.Get<IExceptionHandlerFeature>();
            if (exceptionHandlerFeature != null)
            {
                // Log the exception here if needed
                // Consider injecting ILogger<Error> for proper logging
            }

            RequestId = HttpContext.TraceIdentifier;
        }
    }
}