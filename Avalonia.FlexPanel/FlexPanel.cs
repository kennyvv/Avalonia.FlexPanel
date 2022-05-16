using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.FlexPanel.Enums;
using Avalonia.FlexPanel.Utils;
using Avalonia.VisualTree;

namespace Avalonia.FlexPanel
{

    public class FlexPanel : Panel
    {
        private readonly List<FlexItemInfo> _orderList = new List<FlexItemInfo>();

        private int _lineCount;
        private UVSize _uvConstraint;


        public FlexDirection FlexDirection
        {
            get => GetValue(FlexDirectionProperty);
            set => SetValue(FlexDirectionProperty, value);
        }

        public FlexWrap FlexWrap
        {
            get => GetValue(FlexWrapProperty);
            set => SetValue(FlexWrapProperty, value);
        }

        public FlexJustifyContent JustifyContent
        {
            get => GetValue(JustifyContentProperty);
            set => SetValue(JustifyContentProperty, value);
        }

        public FlexItemsAlignment AlignItems
        {
            get => GetValue(AlignItemsProperty);
            set => SetValue(AlignItemsProperty, value);
        }

        public FlexContentAlignment AlignContent
        {
            get => GetValue(AlignContentProperty);
            set => SetValue(AlignContentProperty, value);
        }

        protected override Size MeasureOverride(Size constraint)
        {
            var flexDirection = FlexDirection;
            var flexWrap = FlexWrap;

            var curLineSize = new UVSize(flexDirection);
            var panelSize = new UVSize(flexDirection);
            _uvConstraint = new UVSize(flexDirection, constraint);
            var childConstraint = new Size(constraint.Width, constraint.Height);
            _lineCount = 1;
            var children = Children;

            _orderList.Clear();

            for (var i = 0; i < children.Count; i++)
            {
                var child = children[i];

                if (child == null) continue;

                _orderList.Add(new FlexItemInfo(i, GetOrder(child)));
            }

            _orderList.Sort();

            for (var i = 0; i < children.Count; i++)
            {
                var child = children[_orderList[i].Index];

                if (child == null) continue;

                var flexBasis = GetFlexBasis(child);
                if (!flexBasis.IsNaN()) child.SetValue(MinWidthProperty, flexBasis);
                child.Measure(childConstraint);

                var sz = new UVSize(flexDirection, child.DesiredSize);

                if (flexWrap == FlexWrap.NoWrap) //continue to accumulate a line
                {
                    curLineSize.U += sz.U;
                    curLineSize.V = Math.Max(sz.V, curLineSize.V);
                }
                else
                {
                    if (MathHelper.GreaterThan(curLineSize.U + sz.U, _uvConstraint.U)) //need to switch to another line
                    {
                        panelSize.U = Math.Max(curLineSize.U, panelSize.U);
                        panelSize.V += curLineSize.V;
                        curLineSize = sz;
                        _lineCount++;

                        if (MathHelper.GreaterThan(
                                sz.U,
                                _uvConstraint.U)) //the element is wider then the constrint - give it a separate line
                        {
                            panelSize.U = Math.Max(sz.U, panelSize.U);
                            panelSize.V += sz.V;
                            curLineSize = new UVSize(flexDirection);
                            _lineCount++;
                        }
                    }
                    else //continue to accumulate a line
                    {
                        curLineSize.U += sz.U;
                        curLineSize.V = Math.Max(sz.V, curLineSize.V);
                    }
                }
            }

            //the last line size, if any should be added
            panelSize.U = Math.Max(curLineSize.U, panelSize.U);
            panelSize.V += curLineSize.V;

            //go from UV space to W/H space
            return new Size(panelSize.Width, panelSize.Height);
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            var flexDirection = FlexDirection;
            var flexWrap = FlexWrap;
            var alignContent = AlignContent;

            var uvFinalSize = new UVSize(flexDirection, arrangeSize);

            if (MathHelper.IsZero(uvFinalSize.U) || MathHelper.IsZero(uvFinalSize.V)) return arrangeSize;

            // init status
            var children = Children;
            var lineIndex = 0;

            var curLineSizeArr = new UVSize[_lineCount];
            curLineSizeArr[0] = new UVSize(flexDirection);

            var lastInLineArr = new int[_lineCount];
            for (var i = 0; i < _lineCount; i++) lastInLineArr[i] = int.MaxValue;

            // calculate line max U space
            for (var i = 0; i < children.Count; i++)
            {
                var child = children[_orderList[i].Index];

                if (child == null) continue;

                var sz = new UVSize(flexDirection, child.DesiredSize);

                if (flexWrap == FlexWrap.NoWrap)
                {
                    curLineSizeArr[lineIndex].U += sz.U;
                    curLineSizeArr[lineIndex].V = Math.Max(sz.V, curLineSizeArr[lineIndex].V);
                }
                else
                {
                    if (MathHelper.GreaterThan(
                            curLineSizeArr[lineIndex].U + sz.U, uvFinalSize.U)) //need to switch to another line
                    {
                        lastInLineArr[lineIndex] = i;
                        lineIndex++;
                        curLineSizeArr[lineIndex] = sz;

                        if (MathHelper.GreaterThan(
                                sz.U,
                                uvFinalSize.U)) //the element is wider then the constraint - give it a separate line
                        {
                            //switch to next line which only contain one element
                            lastInLineArr[lineIndex] = i;
                            lineIndex++;
                            curLineSizeArr[lineIndex] = new UVSize(flexDirection);
                        }
                    }
                    else //continue to accumulate a line
                    {
                        curLineSizeArr[lineIndex].U += sz.U;
                        curLineSizeArr[lineIndex].V = Math.Max(sz.V, curLineSizeArr[lineIndex].V);
                    }
                }
            }

            // init status
            var scaleU = Math.Min(_uvConstraint.U / uvFinalSize.U, 1);
            var firstInLine = 0;
            var wrapReverseAdd = 0;
            var wrapReverseFlag = flexWrap == FlexWrap.WrapReverse ? -1 : 1;
            var accumulatedFlag = flexWrap == FlexWrap.WrapReverse ? 1 : 0;
            var itemsU = .0;
            var accumulatedV = .0;
            var freeV = uvFinalSize.V;
            foreach (var flexSize in curLineSizeArr) freeV -= flexSize.V;

            var freeItemV = freeV;

            // calculate status
            var lineFreeVArr = new double[_lineCount];

            switch (alignContent)
            {
                case FlexContentAlignment.Stretch:
                    if (_lineCount > 1)
                    {
                        freeItemV = freeV / _lineCount;
                        for (var i = 0; i < _lineCount; i++) lineFreeVArr[i] = freeItemV;

                        accumulatedV = flexWrap == FlexWrap.WrapReverse ?
                            uvFinalSize.V - curLineSizeArr[0].V - lineFreeVArr[0] : 0;
                    }

                    break;

                case FlexContentAlignment.FlexStart:
                    wrapReverseAdd = flexWrap == FlexWrap.WrapReverse ? 0 : 1;

                    if (_lineCount > 1)
                        accumulatedV = flexWrap == FlexWrap.WrapReverse ? uvFinalSize.V - curLineSizeArr[0].V : 0;
                    else
                        wrapReverseAdd = 0;

                    break;

                case FlexContentAlignment.FlexEnd:
                    wrapReverseAdd = flexWrap == FlexWrap.WrapReverse ? 1 : 0;

                    if (_lineCount > 1)
                        accumulatedV = flexWrap == FlexWrap.WrapReverse ? uvFinalSize.V - curLineSizeArr[0].V - freeV :
                            freeV;
                    else
                        wrapReverseAdd = 0;

                    break;

                case FlexContentAlignment.Center:
                    if (_lineCount > 1)
                        accumulatedV = flexWrap == FlexWrap.WrapReverse ?
                            uvFinalSize.V - curLineSizeArr[0].V - freeV * 0.5 : freeV * 0.5;

                    break;

                case FlexContentAlignment.SpaceBetween:
                    if (_lineCount > 1)
                    {
                        freeItemV = freeV / (_lineCount - 1);
                        for (var i = 0; i < _lineCount - 1; i++) lineFreeVArr[i] = freeItemV;

                        accumulatedV = flexWrap == FlexWrap.WrapReverse ? uvFinalSize.V - curLineSizeArr[0].V : 0;
                    }

                    break;

                case FlexContentAlignment.SpaceAround:
                    if (_lineCount > 1)
                    {
                        freeItemV = freeV / _lineCount * 0.5;
                        for (var i = 0; i < _lineCount - 1; i++) lineFreeVArr[i] = freeItemV * 2;

                        accumulatedV = flexWrap == FlexWrap.WrapReverse ?
                            uvFinalSize.V - curLineSizeArr[0].V - freeItemV : freeItemV;
                    }

                    break;
            }

            // clear status
            lineIndex = 0;

            // arrange line
            for (var i = 0; i < children.Count; i++)
            {
                var child = children[_orderList[i].Index];

                if (child == null) continue;

                var sz = new UVSize(flexDirection, child.DesiredSize);

                if (flexWrap != FlexWrap.NoWrap)
                    if (i >= lastInLineArr[lineIndex]) //need to switch to another line
                    {
                        ArrangeLine(
                            new FlexLineInfo
                            {
                                ItemsU = itemsU,
                                OffsetV = accumulatedV + freeItemV * wrapReverseAdd,
                                LineV = curLineSizeArr[lineIndex].V,
                                LineFreeV = freeItemV,
                                LineU = uvFinalSize.U,
                                ItemStartIndex = firstInLine,
                                ItemEndIndex = i,
                                ScaleU = scaleU
                            });

                        accumulatedV += (lineFreeVArr[lineIndex] + curLineSizeArr[lineIndex + accumulatedFlag].V)
                                        * wrapReverseFlag;

                        lineIndex++;
                        itemsU = 0;

                        if (i >= lastInLineArr[
                                lineIndex]) //the element is wider then the constraint - give it a separate line
                        {
                            //switch to next line which only contain one element
                            ArrangeLine(
                                new FlexLineInfo
                                {
                                    ItemsU = itemsU,
                                    OffsetV = accumulatedV + freeItemV * wrapReverseAdd,
                                    LineV = curLineSizeArr[lineIndex].V,
                                    LineFreeV = freeItemV,
                                    LineU = uvFinalSize.U,
                                    ItemStartIndex = i,
                                    ItemEndIndex = ++i,
                                    ScaleU = scaleU
                                });

                            accumulatedV += (lineFreeVArr[lineIndex] + curLineSizeArr[lineIndex + accumulatedFlag].V)
                                            * wrapReverseFlag;

                            lineIndex++;
                            itemsU = 0;
                        }

                        firstInLine = i;
                    }

                itemsU += sz.U;
            }

            // arrange the last line, if any
            if (firstInLine < children.Count)
                ArrangeLine(
                    new FlexLineInfo
                    {
                        ItemsU = itemsU,
                        OffsetV = accumulatedV + freeItemV * wrapReverseAdd,
                        LineV = curLineSizeArr[lineIndex].V,
                        LineFreeV = freeItemV,
                        LineU = uvFinalSize.U,
                        ItemStartIndex = firstInLine,
                        ItemEndIndex = children.Count,
                        ScaleU = scaleU
                    });

            return arrangeSize;
        }

        private void ArrangeLine(FlexLineInfo lineInfo)
        {
            var flexDirection = FlexDirection;
            var flexWrap = FlexWrap;
            var justifyContent = JustifyContent;
            var alignItems = AlignItems;

            var isHorizontal = flexDirection == FlexDirection.Row || flexDirection == FlexDirection.RowReverse;
            var isReverse = flexDirection == FlexDirection.RowReverse || flexDirection == FlexDirection.ColumnReverse;
            var itemCount = lineInfo.ItemEndIndex - lineInfo.ItemStartIndex;
            var children = Children;
            var lineFreeU = lineInfo.LineU - lineInfo.ItemsU;
            var constraintFreeU = _uvConstraint.U - lineInfo.ItemsU;

            // calculate initial u
            var u = .0;

            if (isReverse)
                u = justifyContent switch
                {
                    FlexJustifyContent.FlexStart    => lineInfo.LineU,
                    FlexJustifyContent.SpaceBetween => lineInfo.LineU,
                    FlexJustifyContent.SpaceAround  => lineInfo.LineU,
                    FlexJustifyContent.FlexEnd      => lineInfo.ItemsU,
                    FlexJustifyContent.Center       => (lineInfo.LineU + lineInfo.ItemsU) * 0.5,
                    _                               => u
                };
            else
                u = justifyContent switch
                {
                    FlexJustifyContent.FlexEnd => lineFreeU,
                    FlexJustifyContent.Center  => lineFreeU * 0.5,
                    _                          => u
                };

            u *= lineInfo.ScaleU;

            // apply FlexGrow
            var flexGrowUArr = new double[itemCount];

            if (constraintFreeU > 0)
            {
                var ignoreFlexGrow = true;
                var flexGrowSum = .0;

                for (var i = 0; i < itemCount; i++)
                {
                    var flexGrow = GetFlexGrow(children[_orderList[i].Index]);
                    ignoreFlexGrow &= MathHelper.IsVerySmall(flexGrow);
                    flexGrowUArr[i] = flexGrow;
                    flexGrowSum += flexGrow;
                }

                if (!ignoreFlexGrow)
                {
                    var flexGrowItem = .0;

                    if (flexGrowSum > 0)
                    {
                        flexGrowItem = constraintFreeU / flexGrowSum;
                        lineInfo.ScaleU = 1;
                        lineFreeU = 0; //line free U was used up
                    }

                    for (var i = 0; i < itemCount; i++) flexGrowUArr[i] *= flexGrowItem;
                }
                else
                {
                    flexGrowUArr = new double[itemCount];
                }
            }

            // apply FlexShrink
            var flexShrinkUArr = new double[itemCount];

            if (constraintFreeU < 0)
            {
                var ignoreFlexShrink = true;
                var flexShrinkSum = .0;

                for (var i = 0; i < itemCount; i++)
                {
                    var flexShrink = GetFlexShrink(children[_orderList[i].Index]);
                    ignoreFlexShrink &= MathHelper.IsVerySmall(flexShrink - 1);
                    flexShrinkUArr[i] = flexShrink;
                    flexShrinkSum += flexShrink;
                }

                if (!ignoreFlexShrink)
                {
                    var flexShrinkItem = .0;

                    if (flexShrinkSum > 0)
                    {
                        flexShrinkItem = constraintFreeU / flexShrinkSum;
                        lineInfo.ScaleU = 1;
                        lineFreeU = 0; //line free U was used up
                    }

                    for (var i = 0; i < itemCount; i++) flexShrinkUArr[i] *= flexShrinkItem;
                }
                else
                {
                    flexShrinkUArr = new double[itemCount];
                }
            }

            // calculate offset u
            var offsetUArr = new double[itemCount];

            if (lineFreeU > 0)
            {
                if (justifyContent == FlexJustifyContent.SpaceBetween)
                {
                    var freeItemU = lineFreeU / (itemCount - 1);
                    for (var i = 1; i < itemCount; i++) offsetUArr[i] = freeItemU;
                }
                else if (justifyContent == FlexJustifyContent.SpaceAround)
                {
                    if (itemCount == 0)
                        return;

                    var freeItemU = lineFreeU / itemCount * 0.5;
                    offsetUArr[0] = freeItemU;
                    for (var i = 1; i < itemCount; i++) offsetUArr[i] = freeItemU * 2;
                }
            }

            // arrange item
            for (int i = lineInfo.ItemStartIndex, j = 0; i < lineInfo.ItemEndIndex; i++, j++)
            {
                var child = children[_orderList[i].Index];

                if (child == null) continue;

                var childSize = new UVSize(
                    flexDirection,
                    isHorizontal ? new Size(child.DesiredSize.Width * lineInfo.ScaleU, child.DesiredSize.Height) :
                        new Size(child.DesiredSize.Width, child.DesiredSize.Height * lineInfo.ScaleU));

                childSize.U += flexGrowUArr[j] + flexShrinkUArr[j];

                if (isReverse)
                {
                    u -= childSize.U;
                    u -= offsetUArr[j];
                }
                else
                {
                    u += offsetUArr[j];
                }

                var v = lineInfo.OffsetV;
                var alignSelf = GetAlignSelf(child);
                var alignment = alignSelf == FlexItemAlignment.Auto ? alignItems : (FlexItemsAlignment) alignSelf;

                switch (alignment)
                {
                    case FlexItemsAlignment.Stretch:
                        if (_lineCount == 1 && flexWrap == FlexWrap.NoWrap)
                            childSize.V = lineInfo.LineV + lineInfo.LineFreeV;
                        else
                            childSize.V = lineInfo.LineV;

                        break;

                    case FlexItemsAlignment.FlexEnd:
                        v += lineInfo.LineV - childSize.V;

                        break;

                    case FlexItemsAlignment.Center:
                        v += (lineInfo.LineV - childSize.V) * 0.5;

                        break;
                }

                child.Arrange(
                    isHorizontal ? new Rect(u, v, childSize.U, childSize.V) : new Rect(v, u, childSize.V, childSize.U));

                if (!isReverse) u += childSize.U;
            }
        }

        #region Item

        public static readonly StyledProperty<int> OrderProperty =
            AvaloniaProperty.RegisterAttached<FlexPanel, int>("Order", typeof(FlexPanel));

        public static void SetOrder(IControl element, int value)
        {
            element.SetValue(OrderProperty, value);
        }

        public static int GetOrder(IControl element)
        {
            return element.GetValue(OrderProperty);
        }

        public static readonly StyledProperty<double> FlexGrowProperty =
            AvaloniaProperty.RegisterAttached<FlexPanel, double>("FlexGrow", typeof(FlexPanel));

        public static void SetFlexGrow(IControl element, double value)
        {
            element.SetValue(FlexGrowProperty, value);
        }

        public static double GetFlexGrow(IControl element)
        {
            return element.GetValue(FlexGrowProperty);
        }


        public static readonly StyledProperty<double> FlexShrinkProperty =
            AvaloniaProperty.RegisterAttached<FlexPanel, double>("FlexShrink", typeof(FlexPanel));

        public static void SetFlexShrink(IControl element, double value)
        {
            element.SetValue(FlexShrinkProperty, value);
        }

        public static double GetFlexShrink(IControl element)
        {
            return element.GetValue(FlexShrinkProperty);
        }

        public static readonly StyledProperty<double> FlexBasisProperty =
            AvaloniaProperty.RegisterAttached<FlexPanel, double>("FlexBasis", typeof(FlexPanel));

        public static void SetFlexBasis(IControl element, double value)
        {
            element.SetValue(FlexBasisProperty, value);
        }

        public static double GetFlexBasis(IControl element)
        {
            return element.GetValue(FlexBasisProperty);
        }

        public static readonly StyledProperty<FlexItemAlignment> AlignSelfProperty =
            AvaloniaProperty.RegisterAttached<FlexPanel, FlexItemAlignment>("AlignSelf", typeof(FlexPanel));

        public static void SetAlignSelf(IControl element, FlexItemAlignment value)
        {
            element.SetValue(AlignSelfProperty, value);
        }

        #endregion

        #region Panel

        public static FlexItemAlignment GetAlignSelf(IControl element)
        {
            return element.GetValue(AlignSelfProperty);
        }

        public static readonly StyledProperty<FlexDirection> FlexDirectionProperty =
            AvaloniaProperty.RegisterAttached<FlexPanel, FlexDirection>("FlexDirection", typeof(FlexPanel));

        public static readonly StyledProperty<FlexWrap> FlexWrapProperty =
            AvaloniaProperty.RegisterAttached<FlexPanel, FlexWrap>("FlexWrap", typeof(FlexPanel));

        public static readonly StyledProperty<FlexJustifyContent> JustifyContentProperty =
            AvaloniaProperty.RegisterAttached<FlexPanel, FlexJustifyContent>("JustifyContent", typeof(FlexPanel));

        public static readonly StyledProperty<FlexItemsAlignment> AlignItemsProperty =
            AvaloniaProperty.RegisterAttached<FlexPanel, FlexItemsAlignment>("AlignItems", typeof(FlexPanel));

        public static readonly StyledProperty<FlexContentAlignment> AlignContentProperty =
            AvaloniaProperty.RegisterAttached<FlexPanel, FlexContentAlignment>("AlignContent", typeof(FlexPanel));

        #endregion
    }
}