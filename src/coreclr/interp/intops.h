// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _INTOPS_H
#define _INTOPS_H

#include <stdint.h>

typedef enum
{
    InterpOpNoArgs,
    InterpOpInt,
} InterpOpArgType;

#define OPDEF(a,b,c,d,e,f) a,
typedef enum 
{
#include "intops.def"
    INTOP_LAST
} InterpOpcode;
#undef OPDEF

extern uint8_t const interpOpLen[];
extern int const interpOpDVars[];
extern int const interpOpSVars[];
extern InterpOpArgType const interpOpArgType[];
extern const uint8_t* interpNextOp(const uint8_t* ip);

// This, instead of an array of pointers, to optimize away a pointer and a relocation per string.
extern const uint32_t interpOpNameOffsets[];
struct InterpOpNameCharacters;
extern const InterpOpNameCharacters interpOpNameCharacters;

const char* interpOpName(int op);

#endif
