/*
== IPPC - Inter-Process Procedure Calling ==
A neat tool used to call procedures exported by an external module.
The tool is also able to get the result from the call, this can be cast into a managed struct and used from there.

By Alden Viljoen
https://github.com/ald0s
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPPC {
    public class CMain {
        CCoolStuff cool;
        
        public CMain() {
            cool = new CCoolStuff();
        }
        
        public void Begin() {
            while (true) {
                Console.Clear();

                Console.WriteLine("IPPC - Interprocess procedure calling");
                Console.WriteLine("By Alden Viljoen, http://github.com/ald0s");
                Console.WriteLine("");

                Console.WriteLine("Usage: ");
                Console.WriteLine(" - send" + Environment.NewLine + " - get" + Environment.NewLine + " - exit");
                Console.Write("> ");

                string command = Console.ReadLine();
                if(command == "exit") {
                    break;
                }

                ProcessInput(command);

                Console.WriteLine("Operation done! Press ENTER to continue...");
                Console.ReadLine();
            }

            Console.WriteLine("Exited! Thanks for using.");
            Console.ReadLine();
        }

        private void ProcessInput(string input) {
            ProcessInfo_t info = GetProcessHandle();
            if (info.ptrHandle == IntPtr.Zero)
                return;

            switch (input) {
                case "send":
                    PrintInfo(info);
                    return;

                case "get":
                    GetInformation(info);
                    return;

                default:
                    return;
            }
        }

        private void PrintInfo(ProcessInfo_t info) {
            Console.Clear();

            Console.WriteLine("You chose to print information to the example executable.");
            Console.Write("String to send: ");
            string str = Console.ReadLine();

            /* THE COOL STUFF STARTS HERE */
            cool.PrintInfo(info, str);
        }

        private void GetInformation(ProcessInfo_t info) {
            Console.Clear();

            Console.WriteLine("You chose to find out what C++ wants to say!");

            /* THE COOL STUFF STARTS HERE */
            cool.GetInformation(info);
        }

        private ProcessInfo_t GetProcessHandle() {
            ProcessInfo_t info;
            info.ptrBaseAddress = IntPtr.Zero;
            info.ptrHandle = IntPtr.Zero;

            Process[] p = Process.GetProcessesByName("ippp_example");
            if(p.Length == 0) {
                Console.WriteLine("Please open the example binary; 'ippp_example' to continue.");
                return info;
            }

            info.ptrHandle = p[0].Handle;
            info.ptrBaseAddress = p[0].MainModule.BaseAddress;

            return info;
        }
    }

    public struct ProcessInfo_t {
        public IntPtr ptrHandle;
        public IntPtr ptrBaseAddress;
    };
}
