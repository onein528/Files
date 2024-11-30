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
		private Point _originDragPoint;
		private Dictionary<object, System.Drawing.Rectangle> _listViewItemsPosition;
		private List<object> _prevSelectedItems;
		private List<object> _prevSelectedItemsDrag;
		private ItemSelectionStrategy _selectionStrategy;

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
			if (_scrollViewer is null)
                return;

			Canvas.SetLeft(this, 0);
			Canvas.SetTop(this, 0);
			this.Width = 0;
			this.Height = 0;

			_listViewBase.PointerMoved -= ListViewBase_PointerMoved;
			_scrollViewer.ViewChanged -= ScrollViewer_ViewChanged;
			_listViewBase.ReleasePointerCapture(e.Pointer);

			if (SelectionChanged is not null)
			{
				// Restore and trigger SelectionChanged event
				_listViewBase.SelectionChanged -= SelectionChanged;
				_listViewBase.SelectionChanged += SelectionChanged;
				if (_prevSelectedItems is null || !_listViewBase.SelectedItems.SequenceEqual(_prevSelectedItems))
					SelectionChanged(sender, null); // Trigger SelectionChanged event if the selection has changed
			}

			if (_selectionState is SelectionState.Active || e.OriginalSource is ListViewBase)
				OnSelectionEnded(); // Always trigger SelectionEnded to focus the file list when clicking on the empty space (#2977)

			_selectionStrategy = null;
			_selectionState = SelectionState.Inactive;
			_prevSelectedItemsDrag = null;

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

			var currentPoint = e.GetCurrentPoint(_listViewBase);
			var verticalOffset = _scrollViewer.VerticalOffset;

			if (_selectionState is SelectionState.Starting)
			{
				if (!HasMovedMinimalDelta(
                    _originDragPoint.X,
                    _originDragPoint.Y - verticalOffset,
                    currentPoint.Position.X,
                    currentPoint.Position.Y))
					return;

				// Clear selected items once if the pointer is pressed and moved
				_selectionStrategy.StartSelection();
				OnSelectionStarted();
				_selectionState = SelectionState.Active;
			}

			if (currentPoint.Properties.IsLeftButtonPressed)
			{
                // Initial drag point relative to the topleft corner
				var originDragPointShifted = new Point(_originDragPoint.X, _originDragPoint.Y - verticalOffset);
				base.DrawRectangle(currentPoint, originDragPointShifted, _listViewBase);

				// Selected area considering scrolled offset
				var rect = new System.Drawing.Rectangle(
                    (int)Canvas.GetLeft(this),
                    (int)Math.Min(_originDragPoint.Y, currentPoint.Position.Y + verticalOffset),
                    (int)this.Width,
                    (int)Math.Abs(_originDragPoint.Y - (currentPoint.Position.Y + verticalOffset)));

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
						_listViewItemsPosition.Remove(item);
					}
				}
				if (currentPoint.Position.Y > _listViewBase.ActualHeight - 20)
				{
					// Scroll down the list if pointer is at the bottom
					var scrollIncrement = Math.Min(currentPoint.Position.Y - (_listViewBase.ActualHeight - 20), 40);
					_scrollViewer.ChangeView(null, verticalOffset + scrollIncrement, null, false);
				}
				else if (currentPoint.Position.Y < 20)
				{
					// Scroll up the list if pointer is at the top
					var scrollIncrement = Math.Min(20 - currentPoint.Position.Y, 40);
					_scrollViewer.ChangeView(null, verticalOffset - scrollIncrement, null, false);
				}

				if (SelectionChanged is not null)
				{
					var currentSelectedItemsDrag = _listViewBase.SelectedItems.Cast<object>().ToList();
					if (_prevSelectedItemsDrag is null || !_prevSelectedItemsDrag.SequenceEqual(currentSelectedItemsDrag))
					{
                        // Trigger SelectionChanged event if the selection has changed
                        var removedItems = selectedItemsBeforeChange.Except(currentSelectedItemsDrag).ToList();
						SelectionChanged(sender, new SelectionChangedEventArgs(removedItems, currentSelectedItemsDrag));
						_prevSelectedItemsDrag = currentSelectedItemsDrag;
					}
				}
			}
		}

		private void ListViewBase_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			if (_scrollViewer is null)
				return;

			_listViewItemsPosition.Clear();

			_scrollViewer.ViewChanged -= ScrollViewer_ViewChanged;
			_scrollViewer.ViewChanged += ScrollViewer_ViewChanged;

			_originDragPoint = new Point(e.GetCurrentPoint(_listViewBase).Position.X, e.GetCurrentPoint(_listViewBase).Position.Y); // Initial drag point relative to the topleft corner
			_prevSelectedItems = _listViewBase.SelectedItems.Cast<object>().ToList(); // Save current selected items

			var verticalOffset = _scrollViewer.VerticalOffset;
			_originDragPoint.Y += verticalOffset; // Initial drag point relative to the top of the list (considering scrolled offset)
			if (!e.GetCurrentPoint(_listViewBase).Properties.IsLeftButtonPressed || e.Pointer.PointerDeviceType is Microsoft.UI.Input.PointerDeviceType.Touch)
				return; // Trigger only on left click, do not trigger with touch

			FetchItemsPosition();

			_selectionStrategy = e.KeyModifiers.HasFlag(VirtualKeyModifiers.Control)
                ? new InvertPreviousItemSelectionStrategy(_listViewBase.SelectedItems, _prevSelectedItems)
                : e.KeyModifiers.HasFlag(VirtualKeyModifiers.Shift)
                    ? new ExtendPreviousItemSelectionStrategy(_listViewBase.SelectedItems, _prevSelectedItems)
                    : new IgnorePreviousItemSelectionStrategy(_listViewBase.SelectedItems);

			_selectionStrategy.HandleNoItemSelected();

			_listViewBase.PointerMoved -= ListViewBase_PointerMoved;
			_listViewBase.PointerMoved += ListViewBase_PointerMoved;
			if (SelectionChanged is not null)
				_listViewBase.SelectionChanged -= SelectionChanged; // Unsunscribe from SelectionChanged event for performance

			_listViewBase.CapturePointer(e.Pointer);
			_selectionState = SelectionState.Starting;
		}

		private void FetchItemsPosition()
		{
			var verticalOffset = _scrollViewer.VerticalOffset;
			foreach (var item in _listViewBase.Items.ToList().Except(_listViewItemsPosition.Keys))
			{
				var listViewItem = (FrameworkElement)_listViewBase.ContainerFromItem(item);
				if (listViewItem is null)
					continue; // Element is not loaded (virtualized list)

				var gt = listViewItem.TransformToVisual(_listViewBase);
				var itemStartPoint = gt.TransformPoint(new Point(0, verticalOffset)); // Get item position relative to the top of the list (considering scrolled offset)
				var itemRect = new System.Drawing.Rectangle((int)itemStartPoint.X, (int)itemStartPoint.Y, (int)listViewItem.ActualWidth, (int)listViewItem.ActualHeight);
				_listViewItemsPosition[item] = itemRect;
			}
		}
    }
}
