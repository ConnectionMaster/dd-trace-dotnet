#pragma once

#ifdef _WIN32
#include <windows.h>
EXTERN_C IMAGE_DOS_HEADER __ImageBase;
#define HINST_THISCOMPONENT ((HINSTANCE) &__ImageBase)
#else
#define _GNU_SOURCE
#include <dlfcn.h>
#endif

#include "../../../shared/src/native-src/dd_filesystem.hpp"
// namespace fs is an alias defined in "dd_filesystem.hpp"
#include "../../../shared/src/native-src/pal.h"
#include "../../../shared/src/native-src/string.h"

using namespace datadog::shared::nativeloader;

const std::string conf_filename = "loader.conf";
const ::shared::WSTRING cfg_filepath_env = WStr("DD_NATIVELOADER_CONFIGFILE");

static fs::path GetCurrentModuleFolderPath()
{
#ifdef _WIN32
    WCHAR moduleFilePath[MAX_PATH] = {0};
    if (::GetModuleFileName((HMODULE) HINST_THISCOMPONENT, moduleFilePath, MAX_PATH) != 0)
    {
        return fs::path(moduleFilePath).remove_filename();
    }
#else
    Dl_info info;
    if (dladdr((void*)GetCurrentModuleFolderPath, &info))
    {
        return fs::path(info.dli_fname).remove_filename();
    }
#endif
    return {};
}

// Gets the configuration file path
static fs::path GetConfigurationFilePath()
{
    fs::path env_configfile = shared::GetEnvironmentValue(cfg_filepath_env);
    if (!env_configfile.empty())
    {
        return env_configfile;
    }
    else
    {
        return GetCurrentModuleFolderPath() / conf_filename;
    }
}