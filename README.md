# IPPC: Interprocess Procedure Calling
A very interesting tool that can be used to call procedures exported by dynamic link libraries already running within the address space of another process. This exposes many opportunities for useful applications, the project is commented very well in the hopes it'd be easy to understand for you. The code comes in an API format - easy to insert and applicate.

All code where my signature appears is my own, and I ask you treat it as such. You're free to adapt code to your own projects as long as due credit is given. The best way to give credit is to leave my signature where it should be AND/OR provide my GitHub in any sources you release or applications you develop.

### Getting Started
Simply compile the solution with Visual Studio (or your choice of compiler.)
Please keep in mind the application features both a C# and C++ part.

### Quick Demonstration
The following is a brief overview of the functionality of IPPC.
Here, we can see a C# application remotely calling an exported C++ function from within its own address space.
And the C++ application reading the secret string!

#### C# Caller Application
```csharp
struct PrintInfo_t {
	public IntPtr ptrString;
	public int iStringLen;
}
	
public void PrintInfo(ProcessInfo_t info, string strInput) {
	PrintInfo_t print = new PrintInfo_t();
	IntPtr ptrWrittenString = ippc.WriteString(info.ptrHandle, strInput);
	print.iStringLen = strInput.Length;
	print.ptrString = ptrWrittenString;

	IntPtr ptrForeignStruct = ippc.WriteStruct(info.ptrHandle, (object)print);
	IntPtr ptrPrintInfo = ippc.GetRemoteProcAddress(info.ptrHandle, info.ptrBaseAddress, "PrintInfo");

	IntPtr ptrThreadHandle = ippc.Run(info.ptrHandle, ptrPrintInfo, ptrForeignStruct);
	uint uiReturnValue = ippc.GetThreadReturnValue(ptrThreadHandle);

	if(uiReturnValue != 1) {
		Console.WriteLine("Failed to call remote procedure!");
		return;
	}
	
	ippc.FreeMemory(info.ptrHandle, ptrForeignStruct);
	ippc.FreeMemory(info.ptrHandle, ptrWrittenString);
}
```

#### C++ Called Application
```cpp

struct PrintInfo_t {
  DWORD m_pString;
  int m_iLen;
};

extern "C" _declspec(dllexport) unsigned long PrintInfo(void* param);
unsigned long PrintInfo(void* param) {
	if (!param)
		return false;

    PrintInfo_t* print = (PrintInfo_t*)param;

	char* psMemory = new char[print->m_iLen + 1];
	strcpy(psMemory, (char*)print->m_pString);

    printf("String received: %s\n", psMemory);
	delete[] psMemory;

    return true;
}
```

### Prerequisites
* .NET Framework (4.0+)

### Authors
* **Alden Viljoen**
