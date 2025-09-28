using Microsoft.AspNetCore.Components;

namespace FabOS.WebServer.Components.Shared;

public partial class Breadcrumb : ComponentBase
{
    [Parameter] public List<BreadcrumbItem> Items { get; set; } = new();
    [Parameter] public RenderFragment? ChildContent { get; set; }

    public class BreadcrumbItem
    {
        public string Label { get; set; } = "";
        public string Url { get; set; } = "";
        public string Icon { get; set; } = "";
        public bool IsActive { get; set; } = false;
    }
}