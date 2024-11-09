// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System;
using System.Runtime.CompilerServices;
using Windows.Win32;
using Windows.Win32.System.Com;

namespace Windows.Win32
{
	public unsafe class DataExchangeHostWorkaround
	{
        public bool TryHook()
        {
			/*
			var pGetSidSubAuthority  = PInvoke.GetProcAddress(PInvoke.LoadLibrary("KernelBase.dll"), "GetSidSubAuthority");
	
			long res = 0l;
			PInvoke.DetourRestoreAfterWith();
			PInvoke.DetourTransactionBegin();
			res = PInvoke.DetourUpdateThread(PInvoke.GetCurrentThread());
			res = PInvoke.DetourAttach((void**)&pGetSidSubAuthority, &GetSidSubAuthorityHook);
			res = PInvoke.DetourTransactionCommit();

			return res is (long)WIN32_ERROR.NO_ERROR;
			*/
        }

		[UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
		private uint* GetSidSubAuthorityHook(PSID pSid, uint nSubAuthority)
		{
			/*
			uint* ret = pGetSidSubAuthority(pSid, nSubAuthority);

			if (GetWin32LastError() == 0)
			{
				if (*ret >= 0x3000 && *ret < 0x10000)
				{
					uint oriIL = *ret;
					*ret = 0x2000;

					// Medium IL, check condition: (unsigned int)(ilLevel - 0x2000) <= 0xFFF
					//OutputDebugStringA(std::format("[PunchDataExchangeHost] replace TokenIL from 0x{:x} to 0x{:x}", oriIL, *ret).c_str());
				}
			}
			return ret;
			*/

			return null;
		}
	}
}
