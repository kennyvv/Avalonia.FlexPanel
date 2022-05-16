using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.FlexPanel.Enums;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace Avalonia.FlexPanel
{

    /*public class FlexLayout : VirtualizingLayout
    {
        /// <inheritdoc />
        protected override Size MeasureOverride(VirtualizingLayoutContext context, Size availableSize)
        {
            throw new NotImplementedException();
        }
    }*/

    public class FlexPanel : ItemsRepeater
    {
        public FlexDirection FlexDirection
        {
            get => FlexLayout.FlexDirection;
            set => FlexLayout.FlexDirection = value;
        }

        public FlexWrap FlexWrap
        {
            get => FlexLayout.FlexWrap;
            set => FlexLayout.FlexWrap = value;
        }

        public FlexJustifyContent JustifyContent
        {
            get => FlexLayout.JustifyContent;
            set => FlexLayout.JustifyContent = value;
        }

        public FlexItemsAlignment AlignItems
        {
            get => FlexLayout.AlignItems;
            set => FlexLayout.AlignItems = value;
        }

        public FlexContentAlignment AlignContent
        {
            get => FlexLayout.AlignContent;
            set => FlexLayout.AlignContent = value;
        }
        
        private FlexLayout FlexLayout => (FlexLayout)Layout;
        public FlexPanel()
        {
            Layout = new FlexLayout();
            Items = Children;
        }
    }
}