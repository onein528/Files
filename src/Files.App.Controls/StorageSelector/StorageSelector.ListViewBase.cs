// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.System;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;
using DispatcherQueueTimer = Microsoft.UI.Dispatching.DispatcherQueueTimer;

namespace Files.App.Controls
{
    public class StorageSelector
    {
		private ListViewBase _listViewBase;
		private ScrollViewer _scrollViewer;
		private Point originDragPoint;
		private Dictionary<object, System.Drawing.Rectangle> _listViewItemsPosition;
		private List<object> prevSelectedItems;
		private List<object> prevSelectedItemsDrag;
		private ItemSelectionStrategy selectionStrategy;

		private SelectionChangedEventHandler SelectionChanged;

		private void HookEventsForListViewBase()
		{
			if (!_listViewBase.IsLoaded)
			{
				_listViewBase.Loaded += (s, e) => HookEventsForListViewBase();
			}
			else
			{
				_listViewBase.Loaded -= (s, e) => HookEventsForListViewBase();
				_listViewBase.PointerPressed += ListViewBase_PointerPressed;
				_listViewBase.PointerReleased += ListViewBase_PointerReleased;
				_listViewBase.PointerCaptureLost += ListViewBase_PointerReleased;
				_listViewBase.PointerCanceled += ListViewBase_PointerReleased;

				_scrollViewer = DependencyObjectHelpers.FindChild<ScrollViewer>(_listViewBase, sv => sv.VerticalScrollMode is not ScrollMode.Disabled);
				if (_scrollViewer is null)
					_listViewBase.SizeChanged += ListViewBase_SizeChanged;
			}
		}

		private void ScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
		{
			if (!_timer.IsRunning)
				_timer.Debounce(FetchItemsPosition, TimeSpan.FromMilliseconds(1000));
		}

		private void ListViewBase_PointerReleased(object sender, PointerRoutedEventArgs e)
		{
			if (scrollViewer is null) return;
			Canvas.SetLeft(selectionRectangle, 0);
			Canvas.SetTop(selectionRectangle, 0);
			selectionRectangle.Width = 0;
			selectionRectangle.Height = 0;
			_listViewBase.PointerMoved -= RectangleSelection_PointerMoved;

			_scrollViewer.ViewChanged -= ScrollViewer_ViewChanged;
			_listViewBase.ReleasePointerCapture(e.Pointer);
			if (selectionChanged is not null)
			{
				// Restore and trigger SelectionChanged event
				_listViewBase.SelectionChanged -= SelectionChanged;
				_listViewBase.SelectionChanged += SelectionChanged;
				if (prevSelectedItems is null || !_listViewBase.SelectedItems.SequenceEqual(prevSelectedItems))
					selectionChanged(sender, null); // Trigger SelectionChanged event if the selection has changed
			}

			if (selectionState is SelectionState.Active || e.OriginalSource is ListViewBase)
				OnSelectionEnded(); // Always trigger SelectionEnded to focus the file list when clicking on the empty space (#2977)

			selectionStrategy = null;
			selectionState = SelectionState.Inactive;

			prevSelectedItemsDrag = null;

			e.Handled = true;
		}

		private void ListViewBase_SizeChanged(object sender, object e)
		{
			_scrollViewer ??= DependencyObjectHelpers.FindChild<ScrollViewer>(uiElement, sv => sv.VerticalScrollMode != ScrollMode.Disabled);
			if (_scrollViewer is not null)
				_listViewBase.SizeChanged -= ListViewBase_SizeChanged;
		}

		private void ListViewBase_PointerMoved(object sender, PointerRoutedEventArgs e)
		{
			if (_scrollViewer is null)
				return;

			var currentPoint = e.GetCurrentPoint(uiElement);
			var verticalOffset = scrollViewer.VerticalOffset;
			if (_selectionState is SelectionState.Starting)
			{
				if (!HasMovedMinimalDelta(originDragPoint.X, originDragPoint.Y - verticalOffset, currentPoint.Position.X, currentPoint.Position.Y))
					return;

				// Clear selected items once if the pointer is pressed and moved
				_selectionStrategy.StartSelection();
				OnSelectionStarted();
				_selectionState = SelectionState.Active;
			}

			if (currentPoint.Properties.IsLeftButtonPressed)
			{
				var originDragPointShifted = new Point(originDragPoint.X, originDragPoint.Y - verticalOffset); // Initial drag point relative to the topleft corner
				base.DrawRectangle(currentPoint, originDragPointShifted, _listViewBase);

				// Selected area considering scrolled offset
				var rect = new System.Drawing.Rectangle(
                    (int)Canvas.GetLeft(selectionRectangle),
                    (int)Math.Min(originDragPoint.Y, currentPoint.Position.Y + verticalOffset),
                    (int)selectionRectangle.Width,
                    (int)Math.Abs(originDragPoint.Y - (currentPoint.Position.Y + verticalOffset)));

				var selectedItemsBeforeChange = _listViewBase.SelectedItems.ToArray();

				foreach (var item in itemsPosition.ToList())
				{
					try
					{
						if (rect.IntersectsWith(item.Value))
							selectionStrategy.HandleIntersectionWithItem(item.Key);
						else
							selectionStrategy.HandleNoIntersectionWithItem(item.Key);
					}
					catch (ArgumentException)
					{
						// Item is not present in the ItemsSource
						itemsPosition.Remove(item);
					}
				}
				if (currentPoint.Position.Y > uiElement.ActualHeight - 20)
				{
					// Scroll down the list if pointer is at the bottom
					var scrollIncrement = Math.Min(currentPoint.Position.Y - (uiElement.ActualHeight - 20), 40);
					_scrollViewer.ChangeView(null, verticalOffset + scrollIncrement, null, false);
				}
				else if (currentPoint.Position.Y < 20)
				{
					// Scroll up the list if pointer is at the top
					var scrollIncrement = Math.Min(20 - currentPoint.Position.Y, 40);
					_scrollViewer.ChangeView(null, verticalOffset - scrollIncrement, null, false);
				}

				if (selectionChanged is not null)
				{
					var currentSelectedItemsDrag = _listViewBase.SelectedItems.Cast<object>().ToList();
					if (prevSelectedItemsDrag is null || !prevSelectedItemsDrag.SequenceEqual(currentSelectedItemsDrag))
					{
						// Trigger SelectionChanged event if the selection has changed
						var removedItems = selectedItemsBeforeChange.Except(currentSelectedItemsDrag).ToList();
						selectionChanged(sender, new SelectionChangedEventArgs(removedItems, currentSelectedItemsDrag));
						prevSelectedItemsDrag = currentSelectedItemsDrag;
					}
				}
			}
		}

		private void ListViewBase_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			if (_scrollViewer is null)
				return;

			itemsPosition.Clear();

			_scrollViewer.ViewChanged -= ScrollViewer_ViewChanged;
			_scrollViewer.ViewChanged += ScrollViewer_ViewChanged;

			originDragPoint = new Point(e.GetCurrentPoint(_listViewBase).Position.X, e.GetCurrentPoint(_listViewBase).Position.Y); // Initial drag point relative to the topleft corner
			prevSelectedItems = _listViewBase.SelectedItems.Cast<object>().ToList(); // Save current selected items

			var verticalOffset = _scrollViewer.VerticalOffset;
			originDragPoint.Y += verticalOffset; // Initial drag point relative to the top of the list (considering scrolled offset)
			if (!e.GetCurrentPoint(_listViewBase).Properties.IsLeftButtonPressed || e.Pointer.PointerDeviceType is Microsoft.UI.Input.PointerDeviceType.Touch)
				return; // Trigger only on left click, do not trigger with touch

			FetchItemsPosition();

			selectionStrategy = e.KeyModifiers.HasFlag(VirtualKeyModifiers.Control)
                ? new InvertPreviousItemSelectionStrategy(_listViewBase.SelectedItems, prevSelectedItems)
                : e.KeyModifiers.HasFlag(VirtualKeyModifiers.Shift)
                    ? new ExtendPreviousItemSelectionStrategy(_listViewBase.SelectedItems, prevSelectedItems)
                    : new IgnorePreviousItemSelectionStrategy(_listViewBase.SelectedItems);

			selectionStrategy.HandleNoItemSelected();

			_listViewBase.PointerMoved -= ListViewBase_PointerMoved;
			_listViewBase.PointerMoved += ListViewBase_PointerMoved;
			if (selectionChanged is not null)
				_listViewBase.SelectionChanged -= SelectionChanged; // Unsunscribe from SelectionChanged event for performance

			_listViewBase.CapturePointer(e.Pointer);
			selectionState = SelectionState.Starting;
		}

		private void FetchItemsPosition()
		{
			var verticalOffset = _scrollViewer.VerticalOffset;
			foreach (var item in _listViewBase.Items.ToList().Except(itemsPosition.Keys))
			{
				var listViewItem = (FrameworkElement)_listViewBase.ContainerFromItem(item);
				if (listViewItem is null)
					continue; // Element is not loaded (virtualized list)

				var gt = listViewItem.TransformToVisual(_listViewBase);
				var itemStartPoint = gt.TransformPoint(new Point(0, verticalOffset)); // Get item position relative to the top of the list (considering scrolled offset)
				var itemRect = new System.Drawing.Rectangle((int)itemStartPoint.X, (int)itemStartPoint.Y, (int)listViewItem.ActualWidth, (int)listViewItem.ActualHeight);
				itemsPosition[item] = itemRect;
			}
		}
    }
}
