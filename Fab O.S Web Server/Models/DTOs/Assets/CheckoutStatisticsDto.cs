using FabOS.WebServer.Models.Entities.Assets;

namespace FabOS.WebServer.Models.DTOs.Assets;

public class CheckoutStatisticsDto
{
    public int TotalCheckouts { get; set; }
    public int ActiveCheckouts { get; set; }
    public int CompletedCheckouts { get; set; }
    public int OverdueCheckouts { get; set; }
    public int CancelledCheckouts { get; set; }
    public double AverageCheckoutDays { get; set; }
    public int TotalDamageReports { get; set; }
    public Dictionary<int, int> CheckoutsByUser { get; set; } = new();
    public Dictionary<int, int> CheckoutsByKit { get; set; } = new();
}
