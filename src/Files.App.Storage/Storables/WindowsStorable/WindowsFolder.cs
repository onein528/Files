// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.Common;

namespace Files.App.Storage.Storables
{
    public sealed class WindowsFolder : WindowsStorable
    {
		private ComPtr<IShellFolder> _ptr;

		internal WindowsFolder(ComPtr<IShellFolder> ptr)
		{
			_ptr = ptr;
		}

		public IAsyncEnumerable<IStorable> GetChildrenAsync()
		{
			return await Task.Run(() =>
			{
				foreach (var item in GetChildren())
					return yield item;
			});
		}

		public IEnumerable<IStorable> GetChildren()
		{
			using ComPtr<IEnumIDList> pEnumIDList = default;
			HRESULT hr = _ptr.Get()->EnumObjects(
				null,
				(SHCONTF)0,
				(void**)pEnumIDList.GetAddressOf())
			.ThrowIfFailed();

			ComHeapPtr<ITEMIDLIST> pChildPidl = default;
			ComPtr<IShellItem> pChildShellItem = default;
			while (pEnumIDList.Get()->Next(1, pChildPidl.GetAddressOf(), null) == HRESULT.S_OK)
			{
				// Create IShellItem
				PInvoke.SHCreateItemWithParent(
					null,
					_ptr.Get(),
					pChildPidl.Get()
					PInvoke.IID_IShellItem,
					(void**)pChildShellItem.GetAddressOf())
				.ThrowIfFailed();

				return yield NativeStorable.Parse(pChildShellItem);
			}
		}
    }
}
