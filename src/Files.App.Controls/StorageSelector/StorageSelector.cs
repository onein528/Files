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
	public class StorageSelector : Rectangle
	{
		// Fields

		protected readonly double MinSelectionDelta = 5.0;
		protected SelectionState selectionState;

		// Events

		public delegate void SelectionStatusHandler(object sender, EventArgs e);
		public event SelectionStatusHandler SelectionStarted;
		public event SelectionStatusHandler SelectionEnded;

		protected StorageSelector()
		{
		}

		protected void OnSelectionStarted()
		{
			SelectionStarted?.Invoke(this, EventArgs.Empty);
		}

		protected void OnSelectionEnded()
		{
			SelectionEnded?.Invoke(this, EventArgs.Empty);
		}

		protected void DrawRectangle(PointerPoint currentPoint, Point originDragPointShifted, FrameworkElement uiElement)
		{
			// Redraw selection rectangle according to the new point
			if (currentPoint.Position.X >= originDragPointShifted.X)
			{
				double maxWidth = uiElement.ActualWidth - originDragPointShifted.X;
				if (currentPoint.Position.Y <= originDragPointShifted.Y)
				{
					// Pointer was moved up and right
					Canvas.SetLeft(this, Math.Max(0, originDragPointShifted.X));
					Canvas.SetTop(this, Math.Max(0, currentPoint.Position.Y));
					this.Width = Math.Max(0, Math.Min(currentPoint.Position.X - Math.Max(0, originDragPointShifted.X), maxWidth));
					this.Height = Math.Max(0, originDragPointShifted.Y - Math.Max(0, currentPoint.Position.Y));
				}
				else
				{
					// Pointer was moved down and right
					Canvas.SetLeft(this, Math.Max(0, originDragPointShifted.X));
					Canvas.SetTop(this, Math.Max(0, originDragPointShifted.Y));
					this.Width = Math.Max(0, Math.Min(currentPoint.Position.X - Math.Max(0, originDragPointShifted.X), maxWidth));
					this.Height = Math.Max(0, currentPoint.Position.Y - Math.Max(0, originDragPointShifted.Y));
				}
			}
			else
			{
				if (currentPoint.Position.Y <= originDragPointShifted.Y)
				{
					// Pointer was moved up and left
					Canvas.SetLeft(this, Math.Max(0, currentPoint.Position.X));
					Canvas.SetTop(this, Math.Max(0, currentPoint.Position.Y));
					this.Width = Math.Max(0, originDragPointShifted.X - Math.Max(0, currentPoint.Position.X));
					this.Height = Math.Max(0, originDragPointShifted.Y - Math.Max(0, currentPoint.Position.Y));
				}
				else
				{
					// Pointer was moved down and left
					Canvas.SetLeft(this, Math.Max(0, currentPoint.Position.X));
					Canvas.SetTop(this, Math.Max(0, originDragPointShifted.Y));
					this.Width = Math.Max(0, originDragPointShifted.X - Math.Max(0, currentPoint.Position.X));
					this.Height = Math.Max(0, currentPoint.Position.Y - Math.Max(0, originDragPointShifted.Y));
				}
			}
		}

		protected bool HasMovedMinimalDelta(double originalX, double originalY, double currentX, double currentY)
		{
			var deltaX = Math.Abs(originalX - currentX);
			var deltaY = Math.Abs(originalY - currentY);
			return deltaX > MinSelectionDelta || deltaY > MinSelectionDelta;
		}
	}
}
