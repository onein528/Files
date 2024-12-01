// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Files.App.Controls
{
	internal sealed class InvertPreviousItemSelectionStrategy : ItemSelectionStrategy
	{
		private readonly List<object> _prevSelectedItems;

		public InvertPreviousItemSelectionStrategy(ICollection<object> selectedItems, List<object> prevSelectedItems) : base(selectedItems)
		{
			_prevSelectedItems = prevSelectedItems;
		}

		public override void HandleIntersectionWithItem(object item)
		{
			try
			{
				if (_prevSelectedItems.Contains(item))
					_selectedItems.Remove(item);
				else if (!_selectedItems.Contains(item))
					_selectedItems.Add(item);
			}
			catch (COMException) // The list is being modified (#5325)
			{
			}
		}

		public override void HandleNoIntersectionWithItem(object item)
		{
			try
			{
				// Restore selection on items not intersecting with the rectangle
				if (_prevSelectedItems.Contains(item))
				{
					if (!_selectedItems.Contains(item))
						_selectedItems.Add(item);
				}
				else
				{
					_selectedItems.Remove(item);
				}
			}
			catch (COMException) // The list is being modified (#5325)
			{
			}
		}
	}
}
