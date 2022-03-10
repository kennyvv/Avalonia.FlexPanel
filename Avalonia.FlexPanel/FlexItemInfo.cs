namespace Avalonia.FlexPanel;

internal readonly struct FlexItemInfo : IComparable<FlexItemInfo>
{
    public FlexItemInfo(int index, int order)
    {
        Index = index;
        Order = order;
    }

    private int Order { get; }

    public int Index { get; }

    public int CompareTo(FlexItemInfo other)
    {
        var orderCompare = Order.CompareTo(other.Order);
        if (orderCompare != 0) return orderCompare;
        return Index.CompareTo(other.Index);
    }
}