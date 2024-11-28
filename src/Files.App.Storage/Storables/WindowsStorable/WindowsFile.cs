// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.Common;

namespace Files.App.Storage.Storables
{
    public sealed class WindowsFile : WindowsStorable
    {
		private ComPtr<IShellItem> _ptr;

		internal WindowsFile(ComPtr<IShellItem> ptr)
		{
			_ptr = ptr;
		}
    }
}
