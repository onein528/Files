// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System;
using System.Runtime.CompilerServices;
using Windows.Win32;
using Windows.Win32.System.Com;

namespace Windows.Win32
{
	/// <summary>
	/// Contains a heap pointer allocated via CoTaskMemAlloc and a set of methods to work with the pointer safely.
	/// </summary>
	public unsafe static class DataExchangeHostHook
	{
        public bool TryHook()
        {
			/*
			*(void**)&pGetSidSubAuthority = GetProcAddress(LoadLibraryA("KernelBase.dll"), "GetSidSubAuthority");
	
			long res = 0l;
			DetourRestoreAfterWith();
			DetourTransactionBegin();
			res = DetourUpdateThread(GetCurrentThread());
			res = DetourAttach(&(LPVOID&)pGetSidSubAuthority, hook_GetSidSubAuthority);
			res = DetourTransactionCommit();

			return res == (long)WIN32_ERROR.NO_ERROR;
			*/
        }

		private uint* WINAPI hook_GetSidSubAuthority(PSID pSid, uint nSubAuthority)
		{
			/*
			uint* ret = pGetSidSubAuthority(pSid, nSubAuthority);

			if (GetLastError() == 0)
			{
				if (*ret >= 0x3000 && *ret < 0x10000)
				{
					DWORD oriIL = *ret;
					*ret = 0x2000;

					// Medium IL, check condition: (unsigned int)(ilLevel - 0x2000) <= 0xFFF
					OutputDebugStringA(std::format("[PunchDataExchangeHost] replace TokenIL from 0x{:x} to 0x{:x}", oriIL, *ret).c_str());
					SetLastError(0);
				}
			}
			return ret;
			*/
			return null;
		}
	}
}
