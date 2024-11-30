// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;

namespace Files.App.Controls
{
    [DependencyProperty<UIElement>("Binding", nameof(OnBindingPropertyChanged))]
    public class StorageSelector
    {
		protected virtual void OnBindingPropertyChanged(UIElement oldValue, UIElement newValue)
		{
		}
    }
}