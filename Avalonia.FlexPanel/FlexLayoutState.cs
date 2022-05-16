using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.FlexPanel.Enums;
using Avalonia.FlexPanel.Utils;
using Avalonia.Layout;

namespace Avalonia.FlexPanel
{
	public class FlexLayoutState
	{
		private readonly VirtualizingLayoutContext _context;

		private List<FlexItem> _items = new List<FlexItem>();
		public FlexLayoutState(VirtualizingLayoutContext context)
		{
			_context = context;
		}

		internal void InitializeForContext(VirtualizingLayoutContext context)
		{
			//FlowAlgorithm.InitializeForContext(context, callbacks);
			context.LayoutState = this;
		}

		internal void UninitializeForContext(VirtualizingLayoutContext context)
		{
			context.LayoutState = null;
		}

		public FlexItem GetItemAt(int index)
		{
			if (index < 0)
			{
				throw new IndexOutOfRangeException();
			}

			if (index <= (_items.Count - 1))
			{
				return _items[index];
			}
			else
			{
				FlexItem item = new FlexItem(index);
				_items.Add(item);
				return item;
			}
		}
		
		internal void Clear()
		{
			_items.Clear();
		}

		internal void RemoveFromIndex(int index)
		{
			if (index >= _items.Count)
			{
				// Item was added/removed but we haven't realized that far yet
				return;
			}

			int numToRemove = _items.Count - index;
			_items.RemoveRange(index, numToRemove);
		}

		internal void OrderItems()
		{
			for (var i = 0; i < _context.ItemCount; i++)
			{
				FlexItem child = GetItemAt(i);// [i];

				if (child == null) continue;

				var element = _context.GetOrCreateElementAt(i);
				if (element is IControl control)
				{
					child.Element = control;

					child.Order = FlexLayout.GetOrder(control);
					
					child.FlexBasis = FlexLayout.GetFlexBasis(control);
					if (!child.FlexBasis.IsNaN())
						control.SetValue(Layoutable.MinWidthProperty, child.FlexBasis);

					child.FlexGrow = FlexLayout.GetFlexGrow(control);
					child.FlexShrink = FlexLayout.GetFlexShrink(control);
					child.AlignSelf = FlexLayout.GetAlignSelf(control);
					// _orderList.Add(new FlexItemInfo(i, GetOrder(control)));
				}
			}
			
			_items.Sort();
		}
		//internal 
	}

	public class FlexItem : IComparable<FlexItem>
	{
		public FlexItem(int index)
		{
			this.Index = index;
		}

		public int Index { get; }
		public int Order { get; internal set; }
		public double FlexBasis { get; internal set; }
		public double FlexGrow { get; internal set; }
		public double FlexShrink { get; internal set; }
		public FlexItemAlignment AlignSelf { get; internal set; }
		//public UVSize? Size { get; internal set; }

		//public UvMeasure? Position { get; internal set; }

		public IControl Element { get; internal set; }
		
		public int CompareTo(FlexItem other)
		{
			var orderCompare = Order.CompareTo(other.Order);

			if (orderCompare != 0) return orderCompare;

			return Index.CompareTo(other.Index);
		}
	}
}