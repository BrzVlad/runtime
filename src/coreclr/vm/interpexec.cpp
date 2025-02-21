// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "interpexec.h"

#ifdef FEATURE_INTERPRETER

thread_local InterpThreadContext *t_pThreadContext = NULL;

InterpThreadContext* InterpGetThreadContext()
{
    InterpThreadContext *threadContext = t_pThreadContext;

    if (!threadContext)
    {
        threadContext = new InterpThreadContext;
        // FIXME valloc with INTERP_STACK_ALIGNMENT alignment
        threadContext->pStackStart = threadContext->pStackPointer = (int8_t*)malloc(INTERP_STACK_SIZE);
        threadContext->pStackEnd = threadContext->pStackStart + INTERP_STACK_SIZE;

        t_pThreadContext = threadContext;
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

    InterpMethod *pMethod = *(InterpMethod**)pFrame->startIp;
    pThreadContext->pStackPointer = pFrame->pStack + pMethod->allocaSize;
    ip = pFrame->startIp + sizeof(InterpMethod*) / sizeof(int32_t);
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

#define MOV(argtype1,argtype2) \
    LOCAL_VAR(ip [1], argtype1) = LOCAL_VAR(ip [2], argtype2); \
    ip += 3;
            // When loading from a local, we might need to sign / zero extend to 4 bytes
            // which is our minimum "register" size in interp. They are only needed when
            // the address of the local is taken and we should try to optimize them out
            // because the local can't be propagated.
            case INTOP_MOV_I4_I1: MOV(int32_t, int8_t); break;
            case INTOP_MOV_I4_U1: MOV(int32_t, uint8_t); break;
            case INTOP_MOV_I4_I2: MOV(int32_t, int16_t); break;
            case INTOP_MOV_I4_U2: MOV(int32_t, uint16_t); break;
            // Normal moves between vars
            case INTOP_MOV_4: MOV(int32_t, int32_t); break;
            case INTOP_MOV_8: MOV(int64_t, int64_t); break;

            case INTOP_MOV_VT:
                memmove(stack + ip[1], stack + ip[2], ip[3]);
                ip += 4;
                break;

            case INTOP_SWITCH:
            {
                uint32_t val = LOCAL_VAR(ip[1], uint32_t);
                uint32_t n = ip[2];
                ip += 3;
                if (val < n)
                {
                    ip += val;
                    ip += *ip;
                }
                else
                {
                    ip += n;
                }
                break;
            }


#define BR_UNOP(datatype, op)           \
    if (LOCAL_VAR(ip[1], datatype) op)  \
        ip += ip[2];                    \
    else \
        ip += 3;

            case INTOP_BRFALSE_I4:
                BR_UNOP(int32_t, == 0);
                break;
            case INTOP_BRFALSE_I8:
                BR_UNOP(int64_t, == 0);
                break;
            case INTOP_BRTRUE_I4:
                BR_UNOP(int32_t, != 0);
                break;
            case INTOP_BRTRUE_I8:
                BR_UNOP(int64_t, != 0);
                break;

#define BR_BINOP_COND(cond) \
    if (cond)               \
        ip += ip[3];        \
    else                    \
        ip += 4;

#define BR_BINOP(datatype, op) \
    BR_BINOP_COND(LOCAL_VAR(ip[1], datatype) op LOCAL_VAR(ip[2], datatype))

            case INTOP_BEQ_I4:
                BR_BINOP(int32_t, ==);
                break;
            case INTOP_BEQ_I8:
                BR_BINOP(int64_t, ==);
                break;
            case INTOP_BEQ_R4:
            case INTOP_BEQ_R8:
                // TODO Floating point comparisons
                assert(0);
                break;
            case INTOP_BGE_I4:
                BR_BINOP(int32_t, >=);
                break;
            case INTOP_BGE_I8:
                BR_BINOP(int64_t, >=);
                break;
            case INTOP_BGE_R4:
            case INTOP_BGE_R8:
                assert(0);
                break;
            case INTOP_BGT_I4:
                BR_BINOP(int32_t, >);
                break;
            case INTOP_BGT_I8:
                BR_BINOP(int64_t, >);
                break;
            case INTOP_BGT_R4:
            case INTOP_BGT_R8:
                assert(0);
                break;
            case INTOP_BLT_I4:
                BR_BINOP(int32_t, <);
                break;
            case INTOP_BLT_I8:
                BR_BINOP(int64_t, <);
                break;
            case INTOP_BLT_R4:
            case INTOP_BLT_R8:
                assert(0);
                break;
            case INTOP_BLE_I4:
                BR_BINOP(int32_t, <=);
                break;
            case INTOP_BLE_I8:
                BR_BINOP(int64_t, <=);
                break;
            case INTOP_BLE_R4:
            case INTOP_BLE_R8:
                assert(0);
                break;

            case INTOP_BNE_UN_I4:
                BR_BINOP(uint32_t, !=);
                break;
            case INTOP_BNE_UN_I8:
                BR_BINOP(uint64_t, !=);
                break;
            case INTOP_BNE_UN_R4:
            case INTOP_BNE_UN_R8:
                assert(0);
                break;
            case INTOP_BGE_UN_I4:
                BR_BINOP(uint32_t, >=);
                break;
            case INTOP_BGE_UN_I8:
                BR_BINOP(uint64_t, >=);
                break;
            case INTOP_BGE_UN_R4:
            case INTOP_BGE_UN_R8:
                assert(0);
                break;
            case INTOP_BGT_UN_I4:
                BR_BINOP(uint32_t, >);
                break;
            case INTOP_BGT_UN_I8:
                BR_BINOP(uint64_t, >);
                break;
            case INTOP_BGT_UN_R4:
            case INTOP_BGT_UN_R8:
                assert(0);
                break;
            case INTOP_BLE_UN_I4:
                BR_BINOP(uint32_t, <=);
                break;
            case INTOP_BLE_UN_I8:
                BR_BINOP(uint64_t, <=);
                break;
            case INTOP_BLE_UN_R4:
            case INTOP_BLE_UN_R8:
                assert(0);
                break;
            case INTOP_BLT_UN_I4:
                BR_BINOP(uint32_t, <);
                break;
            case INTOP_BLT_UN_I8:
                BR_BINOP(uint64_t, <);
                break;
            case INTOP_BLT_UN_R4:
            case INTOP_BLT_UN_R8:
                assert(0);
                break;

            case INTOP_CONV_R_UN_I4:
                LOCAL_VAR(ip[1], double) = (double)LOCAL_VAR(ip[2], uint32_t);
                ip += 3;
                break;
            case INTOP_CONV_R_UN_I8:
                LOCAL_VAR(ip[1], double) = (double)LOCAL_VAR(ip[2], uint64_t);
                ip += 3;
                break;
            case INTOP_CONV_I1_I4:
                LOCAL_VAR(ip[1], int32_t) = (int8_t)LOCAL_VAR(ip[2], int32_t);
                ip += 3;
                break;
            case INTOP_CONV_I1_I8:
                LOCAL_VAR(ip[1], int32_t) = (int8_t)LOCAL_VAR(ip[2], int64_t);
                ip += 3;
                break;
            case INTOP_CONV_I1_R4:
                LOCAL_VAR(ip[1], int32_t) = (int8_t)(int32_t)LOCAL_VAR(ip[2], float);
                ip += 3;
                break;
            case INTOP_CONV_I1_R8:
                LOCAL_VAR(ip[1], int32_t) = (int8_t)(int32_t)LOCAL_VAR(ip[2], double);
                ip += 3;
                break;
            case INTOP_CONV_U1_I4:
                LOCAL_VAR(ip[1], int32_t) = (uint8_t)LOCAL_VAR(ip[2], int32_t);
                ip += 3;
                break;
            case INTOP_CONV_U1_I8:
                LOCAL_VAR(ip[1], int32_t) = (uint8_t)LOCAL_VAR(ip[2], int64_t);
                ip += 3;
                break;
            case INTOP_CONV_U1_R4:
                LOCAL_VAR(ip[1], int32_t) = (uint8_t)(uint32_t)LOCAL_VAR(ip[2], float);
                ip += 3;
                break;
            case INTOP_CONV_U1_R8:
                LOCAL_VAR(ip[1], int32_t) = (uint8_t)(uint32_t)LOCAL_VAR(ip[2], double);
                ip += 3;
                break;
            case INTOP_CONV_I2_I4:
                LOCAL_VAR(ip[1], int32_t) = (int16_t)LOCAL_VAR(ip[2], int32_t);
                ip += 3;
                break;
            case INTOP_CONV_I2_I8:
                LOCAL_VAR(ip[1], int32_t) = (int16_t)LOCAL_VAR(ip[2], int64_t);
                ip += 3;
                break;
            case INTOP_CONV_I2_R4:
                LOCAL_VAR(ip[1], int32_t) = (int16_t)(int32_t)LOCAL_VAR(ip[2], float);
                ip += 3;
                break;
            case INTOP_CONV_I2_R8:
                LOCAL_VAR(ip[1], int32_t) = (int16_t)(int32_t)LOCAL_VAR(ip[2], double);
                ip += 3;
                break;
            case INTOP_CONV_U2_I4:
                LOCAL_VAR(ip[1], int32_t) = (uint16_t)LOCAL_VAR(ip[2], int32_t);
                ip += 3;
                break;
            case INTOP_CONV_U2_I8:
                LOCAL_VAR(ip[1], int32_t) = (uint16_t)LOCAL_VAR(ip[2], int64_t);
                ip += 3;
                break;
            case INTOP_CONV_U2_R4:
                LOCAL_VAR(ip[1], int32_t) = (uint16_t)(uint32_t)LOCAL_VAR(ip[2], float);
                ip += 3;
                break;
            case INTOP_CONV_U2_R8:
                LOCAL_VAR(ip[1], int32_t) = (uint16_t)(uint32_t)LOCAL_VAR(ip[2], double);
                ip += 3;
                break;
            case INTOP_CONV_I4_R4:
                LOCAL_VAR(ip[1], int32_t) = (int32_t)LOCAL_VAR(ip[2], float);
                ip += 3;
                break;;
            case INTOP_CONV_I4_R8:
                LOCAL_VAR(ip[1], int32_t) = (int32_t)LOCAL_VAR(ip[2], double);
                ip += 3;
                break;;

            case INTOP_CONV_U4_R4:
            case INTOP_CONV_U4_R8:
                assert(0);
                break;

            case INTOP_CONV_I8_I4:
                LOCAL_VAR(ip[1], int64_t) = LOCAL_VAR(ip[2], int32_t);
                ip += 3;
                break;
            case INTOP_CONV_I8_U4:
                LOCAL_VAR(ip[1], int64_t) = (uint32_t)LOCAL_VAR (ip[2], int32_t);
                ip += 3;
                break;;
            case INTOP_CONV_I8_R4:
                LOCAL_VAR(ip[1], int64_t) = (int64_t)LOCAL_VAR(ip[2], float);
                ip += 3;
                break;
            case INTOP_CONV_I8_R8:
                LOCAL_VAR(ip[1], int64_t) = (int64_t)LOCAL_VAR(ip[2], double);
                ip += 3;
                break;
            case INTOP_CONV_R4_I4:
                LOCAL_VAR(ip[1], float) = (float)LOCAL_VAR(ip[2], int32_t);
                ip += 3;
                break;;
            case INTOP_CONV_R4_I8:
                LOCAL_VAR(ip[1], float) = (float)LOCAL_VAR(ip[2], int64_t);
                ip += 3;
                break;
            case INTOP_CONV_R4_R8:
                LOCAL_VAR(ip[1], float) = (float)LOCAL_VAR(ip[2], double);
                ip += 3;
                break;
            case INTOP_CONV_R8_I4:
                LOCAL_VAR(ip[1], double) = (double)LOCAL_VAR(ip[2], int32_t);
                ip += 3;
                break;
            case INTOP_CONV_R8_I8:
                LOCAL_VAR(ip[1], double) = (double)LOCAL_VAR(ip[2], int64_t);
                ip += 3;
                break;
            case INTOP_CONV_R8_R4:
                LOCAL_VAR(ip[1], double) = (double)LOCAL_VAR(ip[2], float);
                ip += 3;
                break;

            case INTOP_CONV_U8_R4:
            case INTOP_CONV_U8_R8:
                // TODO
                assert(0);
                break;

            default:
                assert(0);
                break;
        }
    }

EXIT_FRAME:
    pThreadContext->pStackPointer = pFrame->pStack;
}

#endif // FEATURE_INTERPRETER
