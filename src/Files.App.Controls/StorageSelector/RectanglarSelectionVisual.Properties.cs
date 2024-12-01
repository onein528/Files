// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;

namespace Files.App.Controls
{
    [DependencyProperty<UIElement>("Binding", nameof(OnBindingPropertyChanged))]
    public class RectanglarSelectionVisual
    {
		protected virtual void OnBindingPropertyChanged(UIElement oldValue, UIElement newValue)
		{
            if (newValue is ListViewBase listViewBase)
            {
			    _listViewBase = listViewBase;
			    _listViewItemsPosition = [];
			    HookEventsForListViewBase();
            }
		}
    }
}
