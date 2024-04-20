#pragma once
#include <cstdint>
#include <memory>

#ifdef _STATIC_LIB
    #define API
#else
    #if(_WIN32 || _WIN64)
        #include <SDKDDKVer.h>
        #define API extern "C"  __declspec(dllexport)
    #else
        #define API extern "C" __attribute__((visibility("default")))
    #endif
#endif


// suppress errors in libtorrent headers
#pragma warning(disable : 4996)

