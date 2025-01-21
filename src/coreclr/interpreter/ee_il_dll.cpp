// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include "corjit.h"

#include "interpreter.h"
#include "ee_il_dll.hpp"

#include <assert.h>
#include <string.h>
#include <stdio.h>

/*****************************************************************************/
ICorJitHost* g_interpHost        = nullptr;
bool         g_interpInitialized = false;
/*****************************************************************************/
extern "C" void jitStartup(ICorJitHost* jitHost)
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
extern "C" ICorJitCompiler* getJit()
{
    if (!g_interpInitialized)
    {
        return nullptr;
    }
    return &g_CILInterp;
}

static Executor g_Executor;
extern "C" ICorInterpreter* getInterpreter()
{
    if (!g_interpInitialized)
    {
        return nullptr;
    }
    return &g_Executor;
}

//****************************************************************************
CorJitResult CILInterp::compileMethod(ICorJitInfo*         compHnd,
                                   CORINFO_METHOD_INFO* methodInfo,
                                   unsigned             flags,
                                   uint8_t**            entryAddress,
                                   uint32_t*            nativeSizeOfCode)
{

    const char *methodName = compHnd->getMethodNameFromMetadata(methodInfo->ftn, nullptr, nullptr, nullptr, 0);

    // TODO: replace this by something like the JIT does to support multiple methods being specified and we don't
    // keep fetching it on each call to compileMethod
    const char *methodToInterpret = g_interpHost->getStringConfigValue("AltJit");
    bool doInterpret = (methodName != NULL && strcmp(methodName, methodToInterpret) == 0);
    g_interpHost->freeStringConfigValue(methodToInterpret);

    if (!doInterpret)
    {
        return CORJIT_SKIPPED;
    }

    printf("Interpreter translating method %s\n", methodName);

    // TODO: get rid of the need to allocate fake unwind info.
    compHnd->reserveUnwindInfo(false /* isFunclet */, false /* isColdCode */ , 8 /* unwindSize */);
    AllocMemArgs args;
    args.hotCodeSize = 16;
    args.coldCodeSize = 0;
    args.roDataSize = 0;
    args.xcptnsCount = 0;
    args.flag = CORJIT_ALLOCMEM_FLG_INTERPRETER_CODE;
    compHnd->allocMem(&args);
    uint8_t *code = (uint8_t*)args.hotCodeBlockRW;
    *code++ = 1; // fake byte code

    // TODO: get rid of the need to allocate fake unwind info
    compHnd->allocUnwindInfo((uint8_t*)args.hotCodeBlock, (uint8_t*)args.coldCodeBlock, 0, 1, 0, nullptr, CORJIT_FUNC_ROOT);

    *entryAddress = (uint8_t*)args.hotCodeBlock;
    *nativeSizeOfCode = 1;

    return CORJIT_OK;
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

// TODO: instead of the methodHandle, pass in the IR code address. We can put equivalent of the old InterpMethod* at the beginning of the code.
// Or, how about storing the actual InterpMethod instance at the beginning of the code? Would there be any downside to that?
void Executor::InterpretMethod(CORINFO_METHOD_HANDLE methodHandle, void *pArguments)
{
    //assert(!"Implement the method executor");
}
