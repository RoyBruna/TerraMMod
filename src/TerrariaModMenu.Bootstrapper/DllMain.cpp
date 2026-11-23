#include "ClrHosting.h"

#include <atomic>
#include <cstdio>
#include <cwchar>
#include <string>

namespace
{
    constexpr wchar_t kClrVersion[]      = L"v4.0.30319";
    constexpr wchar_t kCoreAssembly[]    = L"TerrariaModMenu.Core.dll";
    constexpr wchar_t kEntryTypeName[]   = L"TerrariaModMenu.Core.ModEntry";
    constexpr wchar_t kEntryMethodName[] = L"EntryPoint";
    constexpr wchar_t kLogFileName[]     = L"TerrariaModMenu.Bootstrapper.log";

    std::atomic<bool> g_bootstrapped{ false };

    std::wstring GetModuleDirectory(HMODULE hModule)
    {
        wchar_t buffer[MAX_PATH * 2]{};
        DWORD len = GetModuleFileNameW(hModule, buffer, static_cast<DWORD>(std::size(buffer)));
        if (len == 0 || len >= std::size(buffer))
        {
            return L"";
        }

        std::wstring path(buffer, len);
        const size_t pos = path.find_last_of(L"\\/");
        return (pos == std::wstring::npos) ? L"" : path.substr(0, pos + 1);
    }

    void AppendLog(const std::wstring& dir, const char* fmt, ...)
    {
        if (dir.empty())
        {
            return;
        }

        const std::wstring logPath = dir + kLogFileName;
        FILE* file = nullptr;
        if (_wfopen_s(&file, logPath.c_str(), L"a") != 0 || !file)
        {
            return;
        }

        SYSTEMTIME st{};
        GetLocalTime(&st);
        std::fprintf(file, "[%02u:%02u:%02u.%03u] ", st.wHour, st.wMinute, st.wSecond, st.wMilliseconds);

        va_list args;
        va_start(args, fmt);
        std::vfprintf(file, fmt, args);
        va_end(args);

        std::fputc('\n', file);
        std::fclose(file);
    }

    DWORD WINAPI BootstrapThreadProc(LPVOID param)
    {
        auto* hSelf = static_cast<HMODULE>(param);
        const std::wstring baseDir = GetModuleDirectory(hSelf);
        const std::wstring coreDllPath = baseDir + kCoreAssembly;

        AppendLog(baseDir, "Bootstrap thread started (PID=%lu, TID=%lu).", GetCurrentProcessId(), GetCurrentThreadId());

        if (GetFileAttributesW(coreDllPath.c_str()) == INVALID_FILE_ATTRIBUTES)
        {
            AppendLog(baseDir, "ERROR: Managed payload not found next to bootstrapper (%ls).", coreDllPath.c_str());
            return 1;
        }

        HMODULE hMscoree = GetModuleHandleW(L"mscoree.dll");
        if (!hMscoree)
        {
            hMscoree = LoadLibraryW(L"mscoree.dll");
        }
        if (!hMscoree)
        {
            AppendLog(baseDir, "ERROR: Failed to locate/load mscoree.dll (GetLastError=%lu).", GetLastError());
            return 2;
        }

        auto pCLRCreateInstance = reinterpret_cast<ClrHosting::FnCLRCreateInstance>(
            GetProcAddress(hMscoree, "CLRCreateInstance"));
        if (!pCLRCreateInstance)
        {
            AppendLog(baseDir, "ERROR: CLRCreateInstance export not found in mscoree.dll.");
            return 3;
        }

        ClrHosting::ICLRMetaHost* pMetaHost = nullptr;
        ClrHosting::ICLRRuntimeInfo* pRuntimeInfo = nullptr;
        ClrHosting::ICLRRuntimeHost* pRuntimeHost = nullptr;

        HRESULT hr = pCLRCreateInstance(
            ClrHosting::CLSID_CLRMetaHost,
            ClrHosting::IID_ICLRMetaHost,
            reinterpret_cast<LPVOID*>(&pMetaHost));
        if (FAILED(hr) || !pMetaHost)
        {
            AppendLog(baseDir, "ERROR: CLRCreateInstance failed (HRESULT=0x%08lX).", static_cast<unsigned long>(hr));
            return 4;
        }

        hr = pMetaHost->GetRuntime(
            kClrVersion,
            ClrHosting::IID_ICLRRuntimeInfo,
            reinterpret_cast<LPVOID*>(&pRuntimeInfo));
        if (FAILED(hr) || !pRuntimeInfo)
        {
            AppendLog(baseDir, "ERROR: ICLRMetaHost::GetRuntime(%ls) failed (HRESULT=0x%08lX).", kClrVersion, static_cast<unsigned long>(hr));
            pMetaHost->Release();
            return 5;
        }

        BOOL isLoadable = FALSE;
        hr = pRuntimeInfo->IsLoadable(&isLoadable);
        if (FAILED(hr) || !isLoadable)
        {
            AppendLog(baseDir, "ERROR: CLR runtime %ls is not loadable (HRESULT=0x%08lX, IsLoadable=%d).",
                kClrVersion, static_cast<unsigned long>(hr), isLoadable);
            pRuntimeInfo->Release();
            pMetaHost->Release();
            return 6;
        }

        hr = pRuntimeInfo->GetInterface(
            ClrHosting::CLSID_CLRRuntimeHost,
            ClrHosting::IID_ICLRRuntimeHost,
            reinterpret_cast<LPVOID*>(&pRuntimeHost));
        if (FAILED(hr) || !pRuntimeHost)
        {
            AppendLog(baseDir, "ERROR: ICLRRuntimeInfo::GetInterface(ICLRRuntimeHost) failed (HRESULT=0x%08lX).", static_cast<unsigned long>(hr));
            pRuntimeInfo->Release();
            pMetaHost->Release();
            return 7;
        }

        BOOL isStarted = FALSE;
        DWORD startupFlags = 0;
        hr = pRuntimeInfo->IsStarted(&isStarted, &startupFlags);
        if (FAILED(hr) || !isStarted)
        {
            hr = pRuntimeHost->Start();
            if (FAILED(hr))
            {
                AppendLog(baseDir, "ERROR: ICLRRuntimeHost::Start failed (HRESULT=0x%08lX).", static_cast<unsigned long>(hr));
                pRuntimeHost->Release();
                pRuntimeInfo->Release();
                pMetaHost->Release();
                return 8;
            }
        }

        DWORD managedReturn = 0;
        AppendLog(baseDir, "Invoking ExecuteInDefaultAppDomain -> %ls::%ls", kEntryTypeName, kEntryMethodName);
        hr = pRuntimeHost->ExecuteInDefaultAppDomain(
            coreDllPath.c_str(),
            kEntryTypeName,
            kEntryMethodName,
            baseDir.c_str(),
            &managedReturn);

        if (FAILED(hr))
        {
            AppendLog(baseDir, "ERROR: ExecuteInDefaultAppDomain failed (HRESULT=0x%08lX).", static_cast<unsigned long>(hr));
        }
        else
        {
            AppendLog(baseDir, "SUCCESS: Managed EntryPoint returned %lu (HRESULT=0x%08lX).", managedReturn, static_cast<unsigned long>(hr));
        }

        pRuntimeHost->Release();
        pRuntimeInfo->Release();
        pMetaHost->Release();
        return SUCCEEDED(hr) ? 0 : 9;
    }
}

BOOL APIENTRY DllMain(HMODULE hModule, DWORD ulReasonForCall, LPVOID)
{
    if (ulReasonForCall == DLL_PROCESS_ATTACH)
    {
        DisableThreadLibraryCalls(hModule);

        bool expected = false;
        if (g_bootstrapped.compare_exchange_strong(expected, true))
        {
            HANDLE hThread = CreateThread(nullptr, 0, BootstrapThreadProc, hModule, 0, nullptr);
            if (hThread)
            {
                CloseHandle(hThread);
            }
            else
            {
                g_bootstrapped.store(false);
                return FALSE;
            }
        }
    }
    return TRUE;
}
