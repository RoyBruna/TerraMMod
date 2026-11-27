#if !defined(_M_IX86) && !defined(__i386__)
#error "TerrariaModMenu.Injector requires an x86 (Win32) target."
#endif

#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif

#include <windows.h>
#include <tlhelp32.h>

#include <cstdio>
#include <cwchar>
#include <string>

namespace
{
    constexpr wchar_t kDefaultProcessName[] = L"Terraria.exe";
    constexpr wchar_t kBootstrapperDll[]    = L"TerrariaModMenu.Bootstrapper.dll";
    constexpr wchar_t kCoreDll[]            = L"TerrariaModMenu.Core.dll";
    constexpr wchar_t kModeFile[]           = L"launch_mode.cfg";

    enum class LaunchMode
    {
        Ask,
        Full,
        Safe
    };

    struct ScopedHandle
    {
        HANDLE handle = nullptr;

        explicit ScopedHandle(HANDLE h = nullptr) : handle(h) {}
        ~ScopedHandle()
        {
            if (handle && handle != INVALID_HANDLE_VALUE)
            {
                CloseHandle(handle);
            }
        }

        ScopedHandle(const ScopedHandle&) = delete;
        ScopedHandle& operator=(const ScopedHandle&) = delete;

        bool valid() const { return handle != nullptr && handle != INVALID_HANDLE_VALUE; }
    };

    std::wstring GetExecutableDirectory()
    {
        wchar_t buf[MAX_PATH * 2]{};
        DWORD len = GetModuleFileNameW(nullptr, buf, static_cast<DWORD>(std::size(buf)));
        if (len == 0 || len >= std::size(buf))
        {
            return L"";
        }

        std::wstring path(buf, len);
        const size_t pos = path.find_last_of(L"\\/");
        return (pos == std::wstring::npos) ? L"" : path.substr(0, pos + 1);
    }

    std::wstring GetFullPath(const std::wstring& path)
    {
        wchar_t full[MAX_PATH * 2]{};
        DWORD len = GetFullPathNameW(path.c_str(), static_cast<DWORD>(std::size(full)), full, nullptr);
        return (len > 0 && len < std::size(full)) ? std::wstring(full, len) : path;
    }

    bool FileExists(const std::wstring& path)
    {
        const DWORD attrs = GetFileAttributesW(path.c_str());
        return attrs != INVALID_FILE_ATTRIBUTES && (attrs & FILE_ATTRIBUTE_DIRECTORY) == 0;
    }

    std::wstring ResolveDefaultBootstrapperPath()
    {
        const std::wstring exeDir = GetExecutableDirectory();
        const std::wstring candidates[] = {
            exeDir + L"bin\\" + kBootstrapperDll,
            exeDir + kBootstrapperDll,
            exeDir + L"dist\\bin\\" + kBootstrapperDll
        };

        for (const auto& candidate : candidates)
        {
            if (FileExists(candidate))
            {
                return candidate;
            }
        }
        return candidates[0];
    }

    bool WriteLaunchModeFile(const std::wstring& dir, LaunchMode mode)
    {
        const std::wstring cfgPath = dir + kModeFile;
        FILE* f = nullptr;
        if (_wfopen_s(&f, cfgPath.c_str(), L"w") != 0 || !f)
        {
            return false;
        }
        std::fputs(mode == LaunchMode::Safe ? "SAFE\n" : "FULL\n", f);
        std::fclose(f);
        return true;
    }

    LaunchMode PromptLaunchMode()
    {
        std::wprintf(L"=====================================================\n");
        std::wprintf(L"  Terraria Mod Menu Launcher (x86)  -  by RoyBruna\n");
        std::wprintf(L"=====================================================\n");
        std::wprintf(L"  [1] Full Mode (Single-Player)\n");
        std::wprintf(L"      Harmony hooks + all modules enabled\n\n");
        std::wprintf(L"  [2] Safe Mode (Multiplayer / Read-Only)\n");
        std::wprintf(L"      Zero hooks - telemetry, inspector & catalogs only\n");
        std::wprintf(L"=====================================================\n");
        std::wprintf(L"Select mode [1/2] (default 1): ");

        wchar_t line[32]{};
        if (fgetws(line, static_cast<int>(std::size(line)), stdin) != nullptr)
        {
            for (wchar_t ch : line)
            {
                if (ch == L'2' || ch == L's' || ch == L'S')
                {
                    return LaunchMode::Safe;
                }
                if (ch == L'1' || ch == L'f' || ch == L'F' || ch == L'\n' || ch == L'\r')
                {
                    return LaunchMode::Full;
                }
            }
        }
        return LaunchMode::Full;
    }

    DWORD FindProcessIdByName(const wchar_t* processName)
    {
        ScopedHandle snapshot(CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0));
        if (!snapshot.valid())
        {
            return 0;
        }

        PROCESSENTRY32W pe{};
        pe.dwSize = sizeof(pe);
        if (Process32FirstW(snapshot.handle, &pe))
        {
            do
            {
                if (_wcsicmp(pe.szExeFile, processName) == 0)
                {
                    return pe.th32ProcessID;
                }
            } while (Process32NextW(snapshot.handle, &pe));
        }

        return 0;
    }

    bool IsModuleLoaded(DWORD pid, const wchar_t* moduleFileName)
    {
        ScopedHandle snapshot(CreateToolhelp32Snapshot(TH32CS_SNAPMODULE | TH32CS_SNAPMODULE32, pid));
        if (!snapshot.valid())
        {
            return false;
        }

        MODULEENTRY32W me{};
        me.dwSize = sizeof(me);
        if (Module32FirstW(snapshot.handle, &me))
        {
            do
            {
                if (_wcsicmp(me.szModule, moduleFileName) == 0)
                {
                    return true;
                }
            } while (Module32NextW(snapshot.handle, &me));
        }

        return false;
    }

    bool VerifyTargetIs32Bit(HANDLE hTarget)
    {
        BOOL selfIsWow64 = FALSE;
        BOOL targetIsWow64 = FALSE;
        return IsWow64Process(GetCurrentProcess(), &selfIsWow64) &&
               IsWow64Process(hTarget, &targetIsWow64) &&
               selfIsWow64 == targetIsWow64;
    }

    void PauseIfStandaloneConsole(bool forceNoPause)
    {
        if (forceNoPause)
        {
            return;
        }

        DWORD procList[2]{};
        if (GetConsoleProcessList(procList, 2) == 1)
        {
            std::wprintf(L"\nPress ENTER to exit...");
            std::getwchar();
        }
    }

    int RunInjection(const std::wstring& processName, const std::wstring& dllPath, LaunchMode mode)
    {
        const std::wstring fullDllPath = GetFullPath(dllPath);
        const wchar_t* modeName = (mode == LaunchMode::Safe) ? L"SAFE (Multiplayer / Read-Only)" : L"FULL (Single-Player)";

        std::wprintf(L"\nMode   : %ls\n", modeName);
        std::wprintf(L"Target : %ls\n", processName.c_str());
        std::wprintf(L"Module : %ls\n", fullDllPath.c_str());

        if (!FileExists(fullDllPath))
        {
            std::fwprintf(stderr, L"[error] Bootstrapper not found: %ls\n", fullDllPath.c_str());
            return 1;
        }

        const size_t slash = fullDllPath.find_last_of(L"\\/");
        if (slash != std::wstring::npos)
        {
            const std::wstring baseDir = fullDllPath.substr(0, slash + 1);
            const std::wstring corePath = baseDir + kCoreDll;
            if (!FileExists(corePath))
            {
                std::fwprintf(stderr, L"[error] Missing managed assembly: %ls\n", corePath.c_str());
                return 2;
            }
            WriteLaunchModeFile(baseDir, mode);
        }

        const DWORD pid = FindProcessIdByName(processName.c_str());
        if (pid == 0)
        {
            std::fwprintf(stderr, L"[error] Process '%ls' is not running.\n", processName.c_str());
            return 3;
        }
        std::wprintf(L"[ok] PID=%lu\n", pid);

        if (IsModuleLoaded(pid, kBootstrapperDll))
        {
            std::wprintf(L"[info] %ls is already loaded in PID=%lu.\n", kBootstrapperDll, pid);
            return 0;
        }

        constexpr DWORD kAccess =
            PROCESS_CREATE_THREAD |
            PROCESS_QUERY_INFORMATION |
            PROCESS_VM_OPERATION |
            PROCESS_VM_WRITE |
            PROCESS_VM_READ;

        ScopedHandle hProcess(OpenProcess(kAccess, FALSE, pid));
        if (!hProcess.valid())
        {
            std::fwprintf(stderr, L"[error] OpenProcess failed (err=%lu).\n", GetLastError());
            return 4;
        }

        if (!VerifyTargetIs32Bit(hProcess.handle))
        {
            std::fwprintf(stderr, L"[error] Target process architecture mismatch (expected x86).\n");
            return 5;
        }

        const SIZE_T pathBytes = (fullDllPath.size() + 1) * sizeof(wchar_t);
        LPVOID remoteMem = VirtualAllocEx(hProcess.handle, nullptr, pathBytes, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
        if (!remoteMem)
        {
            std::fwprintf(stderr, L"[error] VirtualAllocEx failed (err=%lu).\n", GetLastError());
            return 6;
        }

        SIZE_T written = 0;
        if (!WriteProcessMemory(hProcess.handle, remoteMem, fullDllPath.c_str(), pathBytes, &written) ||
            written != pathBytes)
        {
            std::fwprintf(stderr, L"[error] WriteProcessMemory failed (err=%lu).\n", GetLastError());
            VirtualFreeEx(hProcess.handle, remoteMem, 0, MEM_RELEASE);
            return 7;
        }

        HMODULE hKernel32 = GetModuleHandleW(L"kernel32.dll");
        FARPROC pLoadLibraryW = hKernel32 ? GetProcAddress(hKernel32, "LoadLibraryW") : nullptr;
        if (!pLoadLibraryW)
        {
            std::fwprintf(stderr, L"[error] Failed to resolve kernel32!LoadLibraryW.\n");
            VirtualFreeEx(hProcess.handle, remoteMem, 0, MEM_RELEASE);
            return 8;
        }

        DWORD remoteTid = 0;
        ScopedHandle hRemoteThread(CreateRemoteThread(
            hProcess.handle,
            nullptr,
            0,
            reinterpret_cast<LPTHREAD_START_ROUTINE>(pLoadLibraryW),
            remoteMem,
            0,
            &remoteTid));

        if (!hRemoteThread.valid())
        {
            std::fwprintf(stderr, L"[error] CreateRemoteThread failed (err=%lu).\n", GetLastError());
            VirtualFreeEx(hProcess.handle, remoteMem, 0, MEM_RELEASE);
            return 9;
        }

        const DWORD waitResult = WaitForSingleObject(hRemoteThread.handle, 10000);
        DWORD remoteModuleBase = 0;
        if (waitResult == WAIT_OBJECT_0)
        {
            GetExitCodeThread(hRemoteThread.handle, &remoteModuleBase);
        }

        VirtualFreeEx(hProcess.handle, remoteMem, 0, MEM_RELEASE);

        if (waitResult != WAIT_OBJECT_0)
        {
            std::fwprintf(stderr, L"[error] LoadLibraryW timed out (wait=%lu).\n", waitResult);
            return 10;
        }

        if (remoteModuleBase == 0)
        {
            std::fwprintf(stderr, L"[error] LoadLibraryW returned NULL inside target process.\n");
            return 11;
        }

        std::wprintf(L"[ok] Injected at 0x%08lX (TID=%lu)\n", remoteModuleBase, remoteTid);
        return 0;
    }
}

int wmain(int argc, wchar_t* argv[])
{
    std::wstring processName = kDefaultProcessName;
    std::wstring dllPath = ResolveDefaultBootstrapperPath();
    LaunchMode mode = LaunchMode::Ask;
    bool noPause = false;

    for (int i = 1; i < argc; ++i)
    {
        if (_wcsicmp(argv[i], L"--process") == 0 && i + 1 < argc)
        {
            processName = argv[++i];
        }
        else if (_wcsicmp(argv[i], L"--dll") == 0 && i + 1 < argc)
        {
            dllPath = argv[++i];
        }
        else if (_wcsicmp(argv[i], L"--full") == 0)
        {
            mode = LaunchMode::Full;
        }
        else if (_wcsicmp(argv[i], L"--safe") == 0)
        {
            mode = LaunchMode::Safe;
        }
        else if (_wcsicmp(argv[i], L"--no-pause") == 0)
        {
            noPause = true;
        }
        else if (_wcsicmp(argv[i], L"--help") == 0 || _wcsicmp(argv[i], L"-h") == 0)
        {
            std::wprintf(L"Usage: TerrariaModMenu.exe [--full | --safe] [--process Terraria.exe] [--dll path.dll] [--no-pause]\n");
            return 0;
        }
    }

    if (mode == LaunchMode::Ask)
    {
        mode = PromptLaunchMode();
    }

    const int exitCode = RunInjection(processName, dllPath, mode);
    if (exitCode != 0)
    {
        PauseIfStandaloneConsole(noPause);
    }
    else
    {
        Sleep(250);
    }
    return exitCode;
}
