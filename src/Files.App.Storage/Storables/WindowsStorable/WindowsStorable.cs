// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.Common;

namespace Files.App.Storage.Storables
{
    public abstract class WindowsStorable
    {
		public static IWindowsStorable Parse(string path)
		{
			HRESULT hr = default;

			Guid IID_IShellItem = typeof(IShellItem).GUID;
			ComPtr<IShellItem> pShellItem = default;
			fixed (char* pszPath = path)
				hr = PInvoke.SHCreateItemFromParsingName(
					pszPath,
					null,
					&IID_IShellItem,
					(void**)pShellItem.GetAddressOf());

			return Parse(pShellItem);
		}

		internal static IWindowsStorable Parse(ComPtr<IShellItem> ptr)
		{
			SFGAO attributes = default;
			if (SUCCEEDED(ptr.Get()->GetAttributes(SFGAO.SFGAO_FOLDER, &attributes)) && attributes & SFGAO.SFGAO_FOLDER)
			{
				ComPtr<IShellFolder> pShellFolder = ptr.As<IShellFolder>();
				ptr.Release();

				return WindowsFolder(pShellFolder);
			}
			else
			{
				return WindowsFile(ptr);
			}
		}
    }
}
