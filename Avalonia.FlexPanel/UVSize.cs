using Avalonia.FlexPanel.Enums;

namespace Avalonia.FlexPanel;

internal struct UVSize
{
    public UVSize(FlexDirection direction, Size size)
    {
        U = V = 0d;
        FlexDirection = direction;
        Width = size.Width;
        Height = size.Height;
    }

    public UVSize(FlexDirection direction)
    {
        U = V = 0d;
        FlexDirection = direction;
    }

    public double U { get; set; }

    public double V { get; set; }

    private FlexDirection FlexDirection { get; }

    public double Width
    {
        get => FlexDirection == FlexDirection.Row || FlexDirection == FlexDirection.RowReverse ? U : V;
        private set
        {
            if (FlexDirection == FlexDirection.Row || FlexDirection == FlexDirection.RowReverse)
            {
                U = value;
            }
            else
            {
                V = value;
            }
        }
    }

    public double Height
    {
        get => FlexDirection == FlexDirection.Row || FlexDirection == FlexDirection.RowReverse ? V : U;
        private set
        {
            if (FlexDirection == FlexDirection.Row || FlexDirection == FlexDirection.RowReverse)
            {
                V = value;
            }
            else
            {
                U = value;
            }
        }
    }
}