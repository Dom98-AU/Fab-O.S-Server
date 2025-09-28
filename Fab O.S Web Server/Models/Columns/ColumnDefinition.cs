using System;

namespace FabOS.WebServer.Models.Columns;

public class ColumnDefinition : ICloneable
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string PropertyName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public ColumnType Type { get; set; } = ColumnType.Text;
    public bool IsVisible { get; set; } = true;
    public bool IsSortable { get; set; } = true;
    public bool IsResizable { get; set; } = true;
    public bool IsReorderable { get; set; } = true;
    public bool IsFrozen { get; set; } = false;
    public FreezePosition FreezePosition { get; set; } = FreezePosition.None;
    public int? Width { get; set; } = 150;
    public int MinWidth { get; set; } = 50;
    public int MaxWidth { get; set; } = 500;
    public int Order { get; set; }
    public string? Format { get; set; }
    public string? CssClass { get; set; }
    public string? HeaderCssClass { get; set; }
    public bool IsRequired { get; set; }
    public string? Tooltip { get; set; }

    public object Clone()
    {
        return new ColumnDefinition
        {
            Id = this.Id,
            PropertyName = this.PropertyName,
            DisplayName = this.DisplayName,
            Type = this.Type,
            IsVisible = this.IsVisible,
            IsSortable = this.IsSortable,
            IsResizable = this.IsResizable,
            IsReorderable = this.IsReorderable,
            IsFrozen = this.IsFrozen,
            FreezePosition = this.FreezePosition,
            Width = this.Width,
            MinWidth = this.MinWidth,
            MaxWidth = this.MaxWidth,
            Order = this.Order,
            Format = this.Format,
            CssClass = this.CssClass,
            HeaderCssClass = this.HeaderCssClass,
            IsRequired = this.IsRequired,
            Tooltip = this.Tooltip
        };
    }
}