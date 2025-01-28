// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "interp.h"
#include "ee_il_dll.hpp"

#ifndef DLLEXPORT
#define DLLEXPORT
#endif // !DLLEXPORT

/*****************************************************************************/

ICorJitHost* g_interpHost        = nullptr;
bool         g_interpInitialized = false;

/*****************************************************************************/

extern "C" DLLEXPORT void jitStartup(ICorJitHost* jitHost)
{
    if (g_interpInitialized)
    {
        return;
    }

    g_interpHost = jitHost;

    // TODO Interp intialization

    g_interpInitialized = true;
}

/*****************************************************************************/

static CILInterp g_CILInterp;

extern "C" DLLEXPORT ICorJitCompiler* getJit()
{
    if (!g_interpInitialized)
    {
        return nullptr;
    }

    return &g_CILInterp;
}

static InterpMethod* interpEntryArg;

static int
interpEntry ()
{
    return 0;
}

static void mono_break () { }

//****************************************************************************

CorJitResult CILInterp::compileMethod(ICorJitInfo*         compHnd,
                                   CORINFO_METHOD_INFO* methodInfo,
                                   unsigned             flags,
                                   uint8_t**            entryAddress,
                                   uint32_t*            nativeSizeOfCode)
{
    char buffer[256];
    size_t requiredBufferSize;

    compHnd->printMethodName(methodInfo->ftn, buffer, 256, &requiredBufferSize);

    if (!strcmp(buffer, "Main"))
    {
        InterpMethod *interpMethod;
        InterpCompiler compiler (compHnd, methodInfo);
        compiler.CompileMethod (&interpMethod);

        interpEntryArg = interpMethod;
        *entryAddress = (uint8_t*)interpEntry;
        *nativeSizeOfCode = 1; // ??
        mono_break ();
        return CorJitResult(CORJIT_OK);
    }

    return CorJitResult(CORJIT_SKIPPED);
}

void CILInterp::ProcessShutdownWork(ICorStaticInfo* statInfo)
{
    g_interpInitialized = false;
}

void CILInterp::getVersionIdentifier(GUID* versionIdentifier)
{
    assert(versionIdentifier != nullptr);
    memcpy(versionIdentifier, &JITEEVersionIdentifier, sizeof(GUID));
}

void CILInterp::setTargetOS(CORINFO_OS os)
{
}
