// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "intops.h"

#include <stddef.h>

// This, instead of an array of pointers, to optimize away a pointer and a relocation per string.
struct InterpOpNameCharacters
{
#define OPDEF(a,b,c,d,e,f) char a[sizeof(b)];
#include "intops.def"
#undef OPDEF
};

const struct InterpOpNameCharacters interpOpNameCharacters = {
#define OPDEF(a,b,c,d,e,f) b,
#include "intops.def"
#undef OPDEF
};

const uint32_t interpOpNameOffsets[] = {
#define OPDEF(a,b,c,d,e,f) offsetof(InterpOpNameCharacters, a),
#include "intops.def"
#undef OPDEF
};

uint8_t const interpOpLen[] = {
#define OPDEF(a,b,c,d,e,f) c,
#include "intops.def"
#undef OPDEF
};

int const interpOpSVars[] = {
#define OPDEF(a,b,c,d,e,f) e,
#include "intops.def"
#undef OPDEF
};

int const interpOpDVars[] = {
#define OPDEF(a,b,c,d,e,f) d,
#include "intops.def"
#undef OPDEF
};

InterpOpArgType const interpOpArgType[] = {
#define OPDEF(a,b,c,d,e,f) f,
#include "intops.def"
#undef OPDEF
};

const uint8_t* interpNextOp(const uint8_t *ip)
{
    int len = interpOpLen[*ip];
    return ip + len;
}

const char*
interpOpName(int op)
{
    return ((const char*)&interpOpNameCharacters) + interpOpNameOffsets[op];
}

