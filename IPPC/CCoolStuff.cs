/*
== IPPC - Inter-Process Procedure Calling ==
A neat tool used to call procedures exported by an external module.
The tool is also able to get the result from the call, this can be cast into a managed struct and used from there.

By Alden Viljoen
https://github.com/ald0s

== CCoolStuff Summary ==
Exposes the two main points of this demonstration.
PrintInfo - calls the exported 'PrintInfo' function in the 'ippp_example' project.
GetInformation - calls the exported 'GetInformation' function in the 'ippp_example' project.

I think this is pretty cool. So I called it CoolStuff.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IPPC {
    public class CCoolStuff {
        private CIPPC ippc;

        public CCoolStuff() {
            ippc = new CIPPC();
        }

        public void PrintInfo(ProcessInfo_t info, string strInput) {
            PrintInfo_t print = new PrintInfo_t();

            // We want to write all data to the foreign process first.
            // Begin with the string.
            IntPtr ptrWrittenString = ippc.WriteString(info.ptrHandle, strInput);
            print.iStringLen = strInput.Length;
            print.ptrString = ptrWrittenString;

            // In a real life scenario, you could just call the function with just ptrWrittenString,
            // but our demo is here to show you how you can write more than JUST a single parameter, with the use of Marshal.StructureToPtr().
            // We've now written our structure to the foreign process and have a pointer in return!
            IntPtr ptrForeignStruct = ippc.WriteStruct(info.ptrHandle, (object)print);

            // Now we need the address of the exported function we want to call.
            // Error check is a MUST here especially, but not included for the sake of a clean demo.
            IntPtr ptrPrintInfo = ippc.GetRemoteProcAddress(info.ptrHandle, info.ptrBaseAddress, "PrintInfo");

            // Now that we have all information, we can safely run this.
            IntPtr ptrThreadHandle = ippc.Run(info.ptrHandle, ptrPrintInfo, ptrForeignStruct);

            // We have a handle to the thread. We need to know when its completed, so we can release our memory.
            uint uiReturnValue = ippc.GetThreadReturnValue(ptrThreadHandle);

            // This will be 1 if it succeeded!
            if(uiReturnValue != 1) {
                Console.WriteLine("Failed to call remote procedure!");
                return;
            }

            // Done!
            ippc.FreeMemory(info.ptrHandle, ptrForeignStruct);
            ippc.FreeMemory(info.ptrHandle, ptrWrittenString);
        }

        public void GetInformation(ProcessInfo_t info) {
            // This is a little different, since the external process allocates the result.
            
            // Straight up get the address of our target procedure.
            // Error check is a MUST here especially, but not included for the sake of a clean demo.
            IntPtr ptrGetInformation = ippc.GetRemoteProcAddress(info.ptrHandle, info.ptrBaseAddress, "GetInformation");

            // Call the procedure and get a return value.
            // The return value will contain a pointer to the result structure.
            IntPtr ptrThreadHandle = ippc.Run(info.ptrHandle, ptrGetInformation, IntPtr.Zero);
            uint uiResult = ippc.GetThreadReturnValue(ptrThreadHandle);

            if(uiResult == 0) {
                Console.WriteLine("The foreign procedure returned NULL.");
                return;
            }

            // Success! We now have a pointer. Time to read it!
            GetInformation_t result = new GetInformation_t();
            result = (GetInformation_t)ippc.ReadStruct(info.ptrHandle, new IntPtr(uiResult), result);

            // Our string is currently a pointer! So we must read this now.
            string strOutput = ippc.ReadString(info.ptrHandle, result.ptrString);

            // result will now contain our information. Let's take a peek!
            Console.WriteLine("C++ says: " + strOutput);
            Console.WriteLine("C++ gave us a number: " + result.iRandomNumber);

            // Done!
            ippc.FreeMemory(info.ptrHandle, result.ptrString);
            ippc.FreeMemory(info.ptrHandle, new IntPtr(uiResult));
        }
    }

    // These are our data carriers. They will need to be common with those declared in the external process space.
    struct PrintInfo_t {
        public IntPtr ptrString;
        public int iStringLen;
    }

    struct GetInformation_t {
        public IntPtr ptrString;
        public int iStringLen;

        public int iRandomNumber;
    }
}
