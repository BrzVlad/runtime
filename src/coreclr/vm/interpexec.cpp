// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "interpexec.h"


thread_local InterpThreadContext *g_pThreadContext = NULL;

InterpThreadContext* InterpGetThreadContext()
{
    InterpThreadContext *threadContext = g_pThreadContext;

    if (!threadContext)
    {
        threadContext = new InterpThreadContext;
        // FIXME valloc with INTERP_STACK_ALIGNMENT alignment
        threadContext->pStackStart = threadContext->pStackPointer = (int8_t*)malloc(INTERP_STACK_SIZE);
        threadContext->pStackEnd = threadContext->pStackStart + INTERP_STACK_SIZE;

        g_pThreadContext = threadContext;
        return threadContext;
    }
    else
    {
        return threadContext;
    }
}

#define LOCAL_VAR_ADDR(offset,type) ((type*)(stack + (offset)))
#define LOCAL_VAR(offset,type) (*LOCAL_VAR_ADDR(offset, type))

void InterpExecMethod(InterpFrame *pFrame, InterpThreadContext *pThreadContext)
{
    const int32_t *ip;
    int8_t *stack;

    pThreadContext->pStackPointer = pFrame->pStack + pFrame->pMethod->allocaSize;
    ip = pFrame->pMethod->pCode;
    stack = pFrame->pStack;

    while (true)
    {
        switch (*ip)
        {
            case INTOP_LDC_I4:
                LOCAL_VAR(ip[1], int32_t) = ip[2];
                ip += 3;
                break;
            case INTOP_RET:
                // Return stack slot sized value
                *(int64_t*)pFrame->pRetVal = LOCAL_VAR(ip[1], int64_t);
                goto EXIT_FRAME;
            case INTOP_RET_VOID:
                goto EXIT_FRAME;
        }
    }

EXIT_FRAME:
    pThreadContext->pStackPointer = pFrame->pStack;
}

