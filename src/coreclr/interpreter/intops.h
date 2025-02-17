// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _INTOPS_H
#define _INTOPS_H

#include "openum.h"
#include <stdint.h>

typedef enum
{
    InterpOpNoArgs,
    InterpOpInt,
} InterpOpArgType;

extern uint8_t const g_interpOpLen[];
extern int const g_interpOpDVars[];
extern int const g_interpOpSVars[];
extern InterpOpArgType const g_interpOpArgType[];
extern const uint8_t* InterpNextOp(const uint8_t* ip);

// This, instead of an array of pointers, to optimize away a pointer and a relocation per string.
extern const uint32_t g_interpOpNameOffsets[];
struct InterpOpNameCharacters;
extern const InterpOpNameCharacters g_interpOpNameCharacters;

const char* InterpOpName(int op);

extern OPCODE_FORMAT const g_CEEOpArgs[];
const char* CEEOpName(OPCODE op);
OPCODE CEEDecodeOpcode(const uint8_t **ip);

#ifdef TARGET_64BIT
#define INTOP_MOV_P INTOP_MOV_8
#else
#define INTOP_MOV_P INTOP_MOV_4
#endif

// Helpers identical to ones used by JIT
// FIXME how to consume GET_UNALIGNED_VAL defines from pal as jit ???
//
//#include "pal_mstypes.h"
//#include "pal_endian.h"

inline uint16_t getU2LittleEndian(const uint8_t* ptr)
{
    return *(uint16_t*)ptr;
}

inline uint32_t getU4LittleEndian(const uint8_t* ptr)
{
    return *(uint32_t*)ptr;
}

inline int16_t getI2LittleEndian(const uint8_t* ptr)
{
    return *(int16_t*)ptr;
}

inline int32_t getI4LittleEndian(const uint8_t* ptr)
{
    return *(int32_t*)ptr;
}

inline int64_t getI8LittleEndian(const uint8_t* ptr)
{
    return *(int64_t*)ptr;
}

inline float getR4LittleEndian(const uint8_t* ptr)
{
    int32_t val = getI4LittleEndian(ptr);
    return *(float*)&val;
}

inline double getR8LittleEndian(const uint8_t* ptr)
{
    int64_t val = getI8LittleEndian(ptr);
    return *(double*)&val;
}

#endif
