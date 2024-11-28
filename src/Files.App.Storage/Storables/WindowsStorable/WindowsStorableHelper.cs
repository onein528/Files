// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.Common;

namespace Files.App.Storage.Storables
{
    public static class WindowsStorableHelper
    {
		public T? GetPropertyValue<T>(string propertyKeyName)
		{
			// ...

			if (T is string)
			{
				return string.Empty;
			}
			else
			{
				return null;
			}
		}
    }
}
