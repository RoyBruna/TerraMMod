#pragma once

#if !defined(_M_IX86) && !defined(__i386__)
#error "TerrariaModMenu.Bootstrapper requires an x86 (Win32) target."
#endif

#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif

#include <windows.h>
#include <unknwn.h>

namespace ClrHosting
{
    static constexpr CLSID CLSID_CLRMetaHost =
        { 0x9280188d, 0x0e8e, 0x4867, { 0xb3, 0x0c, 0x7f, 0xa8, 0x38, 0x84, 0xe8, 0xde } };

    static constexpr IID IID_ICLRMetaHost =
        { 0xd332db9e, 0xb9b3, 0x4125, { 0x82, 0x07, 0xa1, 0x48, 0x84, 0xf5, 0x32, 0x16 } };

    static constexpr IID IID_ICLRRuntimeInfo =
        { 0xbd39d1d2, 0xba2f, 0x486a, { 0x89, 0xb0, 0xb4, 0xb0, 0xcb, 0x46, 0x68, 0x91 } };

    static constexpr CLSID CLSID_CLRRuntimeHost =
        { 0x90f1a06e, 0x7712, 0x4762, { 0x86, 0xb5, 0x7a, 0x5e, 0xba, 0x6b, 0xdb, 0x02 } };

    static constexpr IID IID_ICLRRuntimeHost =
        { 0x90f1a06c, 0x7712, 0x4762, { 0x86, 0xb5, 0x7a, 0x5e, 0xba, 0x6b, 0xdb, 0x02 } };

    typedef HRESULT(STDAPICALLTYPE* FnCLRCreateInstance)(
        REFCLSID clsid,
        REFIID riid,
        LPVOID* ppInterface);

    struct ICLRMetaHost : public IUnknown
    {
        virtual HRESULT STDMETHODCALLTYPE GetRuntime(LPCWSTR pwzVersion, REFIID riid, LPVOID* ppRuntime) = 0;
        virtual HRESULT STDMETHODCALLTYPE GetVersionFromFile(LPCWSTR pwzFilePath, LPWSTR pwzBuffer, DWORD* pcchBuffer) = 0;
        virtual HRESULT STDMETHODCALLTYPE EnumerateInstalledRuntimes(void** ppEnumerator) = 0;
        virtual HRESULT STDMETHODCALLTYPE EnumerateLoadedRuntimes(HANDLE hndProcess, void** ppEnumerator) = 0;
        virtual HRESULT STDMETHODCALLTYPE RequestRuntimeLoadedNotification(void* pCallbackFunction) = 0;
        virtual HRESULT STDMETHODCALLTYPE QueryLegacyV2RuntimeBinding(REFIID riid, LPVOID* ppUnk) = 0;
        virtual HRESULT STDMETHODCALLTYPE ExitProcess(INT32 iExitCode) = 0;
    };

    struct ICLRRuntimeInfo : public IUnknown
    {
        virtual HRESULT STDMETHODCALLTYPE GetVersionString(LPWSTR pwzBuffer, DWORD* pcchBuffer) = 0;
        virtual HRESULT STDMETHODCALLTYPE GetRuntimeDirectory(LPWSTR pwzBuffer, DWORD* pcchBuffer) = 0;
        virtual HRESULT STDMETHODCALLTYPE IsLoaded(HANDLE hndProcess, BOOL* pbLoaded) = 0;
        virtual HRESULT STDMETHODCALLTYPE LoadErrorString(UINT iResourceID, LPWSTR pwzBuffer, DWORD* pcchBuffer, LONG iLocaleID) = 0;
        virtual HRESULT STDMETHODCALLTYPE LoadLibraryShim(LPCWSTR pwzDllName, HMODULE* phndModule) = 0;
        virtual HRESULT STDMETHODCALLTYPE GetProcAddress(LPCSTR pszProcName, LPVOID* ppProc) = 0;
        virtual HRESULT STDMETHODCALLTYPE GetInterface(REFCLSID rclsid, REFIID riid, LPVOID* ppUnk) = 0;
        virtual HRESULT STDMETHODCALLTYPE IsLoadable(BOOL* pbLoadable) = 0;
        virtual HRESULT STDMETHODCALLTYPE SetDefaultStartupFlags(DWORD dwStartupFlags, LPCWSTR pwzHostConfigFile) = 0;
        virtual HRESULT STDMETHODCALLTYPE GetDefaultStartupFlags(DWORD* pdwStartupFlags, LPWSTR pwzHostConfigFile, DWORD* pcchHostConfigFile) = 0;
        virtual HRESULT STDMETHODCALLTYPE BindAsLegacyV2Runtime(void) = 0;
        virtual HRESULT STDMETHODCALLTYPE IsStarted(BOOL* pbStarted, DWORD* pdwStartupFlags) = 0;
    };

    struct ICLRRuntimeHost : public IUnknown
    {
        virtual HRESULT STDMETHODCALLTYPE Start(void) = 0;
        virtual HRESULT STDMETHODCALLTYPE Stop(void) = 0;
        virtual HRESULT STDMETHODCALLTYPE SetHostControl(void* pHostControl) = 0;
        virtual HRESULT STDMETHODCALLTYPE GetCLRControl(void** pCLRControl) = 0;
        virtual HRESULT STDMETHODCALLTYPE UnloadAppDomain(DWORD dwAppDomainId, BOOL fWaitUntilDone) = 0;
        virtual HRESULT STDMETHODCALLTYPE ExecuteInAppDomain(DWORD dwAppDomainId, void* pCallback, void* cookie) = 0;
        virtual HRESULT STDMETHODCALLTYPE GetCurrentAppDomainId(DWORD* pdwAppDomainId) = 0;
        virtual HRESULT STDMETHODCALLTYPE ExecuteApplication(
            LPCWSTR pwzAppFullName,
            DWORD dwManifestPaths,
            LPCWSTR* ppwzManifestPaths,
            DWORD dwActivationData,
            LPCWSTR* ppwzActivationData,
            int* pReturnValue) = 0;
        virtual HRESULT STDMETHODCALLTYPE ExecuteInDefaultAppDomain(
            LPCWSTR pwzAssemblyPath,
            LPCWSTR pwzTypeName,
            LPCWSTR pwzMethodName,
            LPCWSTR pwzArgument,
            DWORD* pReturnValue) = 0;
    };
}
