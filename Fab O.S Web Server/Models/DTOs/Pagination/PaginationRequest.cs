using System.ComponentModel.DataAnnotations;

namespace FabOS.WebServer.Models.DTOs.Pagination;

/// <summary>
/// Pagination request parameters
/// </summary>
public class PaginationRequest
{
    private const int DefaultPageSize = 50;
    private const int MaxPageSize = 200;

    private int _pageNumber = 1;
    private int _pageSize = DefaultPageSize;

    [Range(1, int.MaxValue)]
    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value < 1 ? 1 : value;
    }

    [Range(1, MaxPageSize)]
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value < 1 ? DefaultPageSize : (value > MaxPageSize ? MaxPageSize : value);
    }

    public int Skip => (PageNumber - 1) * PageSize;
}
