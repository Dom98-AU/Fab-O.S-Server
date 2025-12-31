using System.ComponentModel.DataAnnotations;

namespace FabOS.WebServer.Models.DTOs.Pagination;

/// <summary>
/// Non-generic API response for simple success/error messages
/// </summary>
public class ApiResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }

    public static ApiResponse Ok(string? message = null)
    {
        return new ApiResponse { Success = true, Message = message };
    }

    public static ApiResponse Error(string message)
    {
        return new ApiResponse { Success = false, Message = message };
    }
}
