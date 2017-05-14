/*
== IPPC - Inter-Process Procedure Calling ==
A neat tool used to call procedures exported by an external module.
The tool is also able to get the result from the call, this can be cast into a managed struct and used from there.

By Alden Viljoen
https://github.com/ald0s
*/

/*
== FORENOTE ON THIS CODE (DISCLAIMER) ==
This code is very old, wrote by me almost two years ago.
I haven't bothered rewriting this, because it works well as it is,
but it probably is due for a rewrite.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace IPPC {
    public unsafe partial class CIPPC {
        public IntPtr GetRemoteProcAddress(IntPtr ptrHandle, IntPtr hBaseAddress, string sProcName) {
            IMAGE_DOS_HEADER32 dosHeader = new IMAGE_DOS_HEADER32();

            uint uiSignature = 0;
            IMAGE_FILE_HEADER fileHeader = new IMAGE_FILE_HEADER();
            IMAGE_OPTIONAL_HEADER32 optHeader32 = new IMAGE_OPTIONAL_HEADER32();
            IMAGE_DATA_DIRECTORY exportDirectory = new IMAGE_DATA_DIRECTORY();
            IMAGE_EXPORT_DIRECTORY exportTable = new IMAGE_EXPORT_DIRECTORY();

            IntPtr ptrFunctionTable = IntPtr.Zero;
            IntPtr ptrNameTable = IntPtr.Zero;
            IntPtr ptrOrdinalTable = IntPtr.Zero;

            uint[] uiExportFuncTable;
            uint[] uiExportNameTable;
            ushort[] usExportOrdinalTable;

            if (ptrHandle == IntPtr.Zero || hBaseAddress == IntPtr.Zero) {
                Console.WriteLine("Invalid call.");
                return IntPtr.Zero;
            }

            IntPtr ptrNumBytesRead = IntPtr.Zero;
            if (!ReadProcessMemory(ptrHandle, hBaseAddress,
                &dosHeader, Marshal.SizeOf(dosHeader), out ptrNumBytesRead)) {
                Console.WriteLine("Failed. Error code: " + Marshal.GetLastWin32Error().ToString());
                return IntPtr.Zero;
            }

            if (dosHeader.e_magic != 0x5A4D) {
                Console.WriteLine("Image is not a valid DLL. " + dosHeader.e_magic.ToString());
                return IntPtr.Zero;
            }

            if (!ReadProcessMemory(ptrHandle, hBaseAddress + (dosHeader.e_lfanew),
                &uiSignature, Marshal.SizeOf(uiSignature), out ptrNumBytesRead)) {
                Console.WriteLine("Failed. Error code: " + Marshal.GetLastWin32Error().ToString());
                return IntPtr.Zero;
            }

            if (uiSignature != 0x00004550) {
                Console.WriteLine("Invalid NT signature...");
                return IntPtr.Zero;
            }

            if (!ReadProcessMemory(ptrHandle, hBaseAddress + (dosHeader.e_lfanew + Marshal.SizeOf(uiSignature)),
                &fileHeader, Marshal.SizeOf(fileHeader), out ptrNumBytesRead)) {
                Console.WriteLine("Failed. Error code: " + Marshal.GetLastWin32Error().ToString());
                return IntPtr.Zero;
            }

            if (!ReadProcessMemory(ptrHandle, hBaseAddress + (dosHeader.e_lfanew + Marshal.SizeOf(uiSignature) + Marshal.SizeOf(fileHeader)),
                &optHeader32, Marshal.SizeOf(optHeader32), out ptrNumBytesRead)) {
                Console.WriteLine("Failed. Error code: " + Marshal.GetLastWin32Error().ToString());
                return IntPtr.Zero;
            }

            if (optHeader32.NumberOfRvaAndSizes >= 1) {
                exportDirectory.VirtualAddress =
                    (optHeader32.ExportTable.VirtualAddress);
                exportDirectory.Size = (optHeader32.ExportTable.Size);
            } else {
                Console.WriteLine("No export table found.");
                return IntPtr.Zero;
            }

            if (!ReadProcessMemory(ptrHandle, hBaseAddress + (int)(exportDirectory.VirtualAddress),
                &exportTable, Marshal.SizeOf(exportTable), out ptrNumBytesRead)) {
                Console.WriteLine("Failed. Error code: " + Marshal.GetLastWin32Error().ToString());
                return IntPtr.Zero;
            }

            uiExportFuncTable = ReadArray(ptrHandle,
                hBaseAddress + (int)(exportTable.AddressOfFunctions),
                exportTable.NumberOfFunctions);

            uiExportNameTable = ReadArray(ptrHandle,
                hBaseAddress + (int)(exportTable.AddressOfNames),
                exportTable.NumberOfNames);

            usExportOrdinalTable = ReadArrayShort(ptrHandle,
                hBaseAddress + (int)(exportTable.AddressOfNameOrdinals),
                exportTable.NumberOfNames);

            for (int i = 0; i < exportTable.NumberOfNames; i++) {
                string sFuncName = GetProcName(ptrHandle, hBaseAddress + (int)(uiExportNameTable[i]));
                if (sFuncName == sProcName) {
                    IntPtr ptrReturn = new IntPtr((int)hBaseAddress + uiExportFuncTable[usExportOrdinalTable[i]]);
                    return ptrReturn;
                }
            }
            return IntPtr.Zero;
        }

        private string GetProcName(IntPtr hHandle, IntPtr ptrStart) {
            int iIndex = 0;
            string sResult = "";

            fixed (byte* cCurrent = new byte[1]) {
                while (true) {
                    IntPtr ptrNumBytesRead = IntPtr.Zero;
                    if (!ReadProcessMemory(hHandle, ptrStart + iIndex, cCurrent, sizeof(char), out ptrNumBytesRead)) {
                        Console.WriteLine("Couldn't read process memory.");
                        return null;
                    }

                    byte btDecimal = (byte)(*cCurrent);
                    char cChar = Convert.ToChar(btDecimal);

                    if (cChar == '\0') {
                        break;
                    } else {
                        sResult += cChar;
                    }
                    iIndex++;
                }
                return sResult;
            }
        }

        private uint[] ReadArray(IntPtr hHandle, IntPtr ptrTable, uint iNumberToRead) {
            uint[] uiExported = new uint[iNumberToRead];

            fixed (uint* uiLocalTable = new uint[iNumberToRead]) {
                IntPtr pNumBytesRead = IntPtr.Zero;

                if (!ReadProcessMemory(hHandle, ptrTable,
                    uiLocalTable, (int)(iNumberToRead * sizeof(uint)), out pNumBytesRead)) {
                    Console.WriteLine("Failed to read function table.");
                    return null;
                }

                uint* uiAddress = uiLocalTable;
                for (int i = 0; i < iNumberToRead; i++, uiAddress++) {
                    uiExported[i] = *uiAddress;
                }

                return uiExported;
            }
        }

        private ushort[] ReadArrayShort(IntPtr hHandle, IntPtr ptrTable, uint iNumberToRead) {
            ushort[] usExported = new ushort[iNumberToRead];

            fixed (ushort* usLocalTable = new ushort[iNumberToRead]) {
                IntPtr pNumBytesRead = IntPtr.Zero;

                if (!ReadProcessMemory(hHandle, ptrTable,
                    usLocalTable, (int)(iNumberToRead * sizeof(ushort)), out pNumBytesRead)) {
                    Console.WriteLine("Failed to read function table.");
                    return null;
                }

                ushort* usAddress = usLocalTable;
                for (int i = 0; i < iNumberToRead; i++, usAddress++) {
                    usExported[i] = *usAddress;
                }

                return usExported;
            }
        }
    }
}
