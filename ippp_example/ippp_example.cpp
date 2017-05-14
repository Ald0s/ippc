/*
== IPPC - Inter-Process Procedure Calling ==
A neat tool used to call procedures exported by an external module.
The tool is also able to get the result from the call, this can be cast into a managed struct and used from there.

By Alden Viljoen
https://github.com/ald0s
*/

#include <Windows.h>
#include <iostream>
#include <cstdlib>

// Just some test information, this is what we'll pass from
// C# to C++.
struct PrintInfo_t {
  DWORD m_pString;
  int m_iLen;
};

// This is what we'll return to C#.
struct GetInformation_t {
  DWORD m_pString;
  int m_iStringLength;
  
  int m_iRandomNumber;
};

// Declare our prototypes for the exported functions.
// These will be called by our C# implementation.
extern "C" _declspec(dllexport) unsigned long PrintInfo(void* param);
extern "C" _declspec(dllexport) unsigned long GetInformation(void* param);

unsigned long PrintInfo(void* param) {
	if (!param) {
		return false;
	}
        
    // param currently points to the location in our memory where PrintInfo_t is written. A simple cast will work.
	// Now, we can create our own copy of this memory to use later in this process, since as soon as the procedure returns,
	// the sent memory will be cleared.
    PrintInfo_t* print = (PrintInfo_t*)param;

	char* psMemory = new char[print->m_iLen + 1];
	strcpy(psMemory, (char*)print->m_pString);

    printf("String received: %s\n", psMemory);
	delete[] psMemory;

    // C# will free this memory once we've returned.
    return true;
}

unsigned long GetInformation(void* param) {
    // In this instance, we pass nothing in. We only want to receive.
    // Allocate heap memory for this. C# will clear this.
    GetInformation_t* get = (GetInformation_t*)
		VirtualAllocEx(GetCurrentProcess(), 0, sizeof(GetInformation_t), MEM_COMMIT, PAGE_READWRITE);
	
    if(!get)
        return 0;
    
    // Copy over some test data.
	const char* pszTest = "Hello from the other side! (the dynamic link library...)";
	int len = strlen(pszTest);

	char* psString = new char[len + 1];
	strcpy(psString, pszTest);
    
	get->m_pString = (DWORD)psString;
    get->m_iStringLength = len;
	
    get->m_iRandomNumber = rand();
	printf("Our random number is %i!\n", get->m_iRandomNumber);
    
    // Return the pointer.
    // This will eventually end up as the exit code value for the thread, and will be acquired by GetThreadReturnValue()
    // in C#.
    return (unsigned long)get;
}

int main()
{
    printf("IPPC - Interprocess Procedure Calling\nMade by Alden Viljoen - http://github.com/ald0s\n");

	while (true) {
		Sleep(500);
	}

    return 0;
}