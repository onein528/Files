﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Helpers.ContextFlyouts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System.Windows.Input;

namespace Files.App.UserControls.Widgets
{
	/// <summary>
	/// Represents base ViewModel for widget ViewModels.
	/// </summary>
	public abstract class BaseWidgetViewModel : ObservableObject
	{
		// Dependency injections

		protected IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();
		protected IQuickAccessService QuickAccessService { get; } = Ioc.Default.GetRequiredService<IQuickAccessService>();
		protected IStorageService StorageService { get; } = Ioc.Default.GetRequiredService<IStorageService>();
		protected IHomePageContext HomePageContext { get; } = Ioc.Default.GetRequiredService<IHomePageContext>();
		protected IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();
		protected IFileTagsService FileTagsService { get; } = Ioc.Default.GetRequiredService<IFileTagsService>();
		protected DrivesViewModel DrivesViewModel = Ioc.Default.GetRequiredService<DrivesViewModel>();
		protected NetworkDrivesViewModel NetworkDrivesViewModel = Ioc.Default.GetRequiredService<NetworkDrivesViewModel>();

		// Fields

		protected string? _flyoutItemPath;

		// Commands

		protected ICommand RemoveRecentItemCommand { get; set; } = null!;
		protected ICommand ClearAllItemsCommand { get; set; } = null!;
		protected ICommand OpenFileLocationCommand { get; set; } = null!;
		protected ICommand OpenInNewTabCommand { get; set; } = null!;
		protected ICommand OpenInNewWindowCommand { get; set; } = null!;
		protected ICommand OpenPropertiesCommand { get; set; } = null!;
		protected ICommand PinToSidebarCommand { get; set; } = null!;
		protected ICommand UnpinFromSidebarCommand { get; set; } = null!;

		// Events

		public static event EventHandler<WidgetsRightClickedItemChangedEventArgs>? RightClickedItemChanged;

		// Methods

		public abstract List<ContextMenuFlyoutItemViewModel> GetItemMenuItems(WidgetCardItem item, bool isPinned, bool isFolder = false);

		public void BuildItemContextMenu(object sender, RightTappedRoutedEventArgs e)
		{
			// Ensure values are not null
			if (sender is not FrameworkElement widgetCardItem ||
				widgetCardItem.DataContext is not WidgetCardItem item ||
				item.Path is null)
				return;

			// Create a new Flyout
			var itemContextMenuFlyout = new CommandBarFlyout()
			{
				Placement = FlyoutPlacementMode.Full
			};

			// Hook events
			itemContextMenuFlyout.Opening += (sender, e) => App.LastOpenedFlyout = sender as CommandBarFlyout;
			itemContextMenuFlyout.Closed += (sender, e) => OnRightClickedItemChanged(null, null);

			_flyoutItemPath = item.Path;

			// Notify of the change on right clicked item
			OnRightClickedItemChanged(item, itemContextMenuFlyout);

			// Get items for the flyout
			var menuItems = GetItemMenuItems(item, QuickAccessService.IsItemPinned(item.Path));
			var (_, secondaryElements) = ContextFlyoutModelToElementHelper.GetAppBarItemsFromModel(menuItems);

			// Set max width of the flyout
			secondaryElements
				.OfType<FrameworkElement>()
				.ForEach(i => i.MinWidth = Constants.UI.ContextMenuItemsMaxWidth);

			// Add menu items to the secondary flyout
			secondaryElements.ForEach(itemContextMenuFlyout.SecondaryCommands.Add);

			// Show the flyout
			itemContextMenuFlyout.ShowAt(widgetCardItem, new() { Position = e.GetPosition(widgetCardItem) });

			// Load shell menu items
			_ = ShellContextFlyoutFactory.LoadShellMenuItemsAsync(_flyoutItemPath, itemContextMenuFlyout);

			e.Handled = true;
		}

		// Command methods

		public async Task ExecuteOpenInNewTabCommand(WidgetCardItem? item)
		{
			await NavigationHelpers.OpenPathInNewTab(item?.Path ?? string.Empty, false);
		}

		public async Task ExecuteOpenInNewWindowCommand(WidgetCardItem? item)
		{
			await NavigationHelpers.OpenPathInNewWindowAsync(item?.Path ?? string.Empty);
		}

		public virtual async Task ExecutePinToSidebarCommand(WidgetCardItem? item)
		{
			await QuickAccessService.PinToSidebarAsync(item?.Path ?? string.Empty);
		}

		public virtual async Task ExecuteUnpinFromSidebarCommand(WidgetCardItem? item)
		{
			await QuickAccessService.UnpinFromSidebarAsync(item?.Path ?? string.Empty);
		}

		protected void OnRightClickedItemChanged(WidgetCardItem? item, CommandBarFlyout? flyout)
		{
			RightClickedItemChanged?.Invoke(this, new WidgetsRightClickedItemChangedEventArgs(item, flyout));
		}
	}
}
