// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include "interp.h"

#include "openum.h"

enum StackType {
    StackTypeI4 = 0,
    StackTypeI8,
    StackTypeR4,
    StackTypeR8,
    StackTypeO,
    StackTypeVT,
    StackTypeMP,
    StackTypeF
};

// Types relevant for interpreter vars and opcodes. They are used in the final
// stages of the codegen and can be used during execution.
enum InterpType {
    InterpTypeI1 = 0,
    InterpTypeU1,
    InterpTypeI2,
    InterpTypeU2,
    InterpTypeI4,
    InterpTypeI8,
    InterpTypeR4,
    InterpTypeR8,
    InterpTypeO,
    InterpTypeVT,
    InterpTypeVOID,
#ifdef TARGET_64BIT
    InterpTypeI = InterpTypeI8
#else
    InterpTypeI = InterpTypeI4
#endif
};

static int stackTypeFromInterpType [] =
{
    StackTypeI4, // I1
    StackTypeI4, // U1
    StackTypeI4, // I2
    StackTypeI4, // U2
    StackTypeI4, // I4
    StackTypeI8, // I8
    StackTypeR4, // R4
    StackTypeR8, // R8
    StackTypeO,  // O
    StackTypeVT  // VT
};

static int interpTypeFromStackType [] =
{
    InterpTypeI4, // I4,
    InterpTypeI8, // I8,
    InterpTypeR4, // R4,
    InterpTypeR8, // R8,
    InterpTypeO,  // O,
    InterpTypeVT, // VT,
    InterpTypeI,  // MP,
    InterpTypeI,  // F
};


// Fast allocator for small chunks of memory that can be freed together when the
// method compilation is finished.
void* InterpCompiler::allocMemPool(size_t numBytes)
{
    // FIXME This should instead allocate from a mempool that is freed once
    // method compilation is finished.
    return malloc(numBytes);
}

// Allocator for potentially larger chunks of data, that we might want to free
// eagerly to prevent excessive memory usage.
void* InterpCompiler::allocTemporary(size_t numBytes)
{
    return malloc(numBytes);
}

void* InterpCompiler::reallocTemporary(void* ptr, size_t numBytes)
{
    return realloc(ptr, numBytes);
}

void InterpCompiler::freeTemporary(void* ptr)
{
    free(ptr);
}

static int getDataLen(int opcode)
{
    int length = interpOpLen[opcode];
    int numSVars = interpOpDVars[opcode];
    int numDVars = interpOpDVars[opcode];

    return length - 1 - numSVars - numDVars;
}

InterpInst* InterpCompiler::addIns(int opcode)
{
    return addInsExplicit(opcode, getDataLen(opcode));
}

InterpInst* InterpCompiler::addInsExplicit(int opcode, int dataLen)
{
    InterpInst *ins = newIns(opcode, dataLen);
    ins->prev = cbb->lastIns;
    if (cbb->lastIns)
        cbb->lastIns->next = ins;
    else
        cbb->firstIns = ins;
    cbb->lastIns = ins;
    return ins;
}

InterpInst* InterpCompiler::newIns(int opcode, int dataLen)
{
    InterpInst *ins;
    ins = (InterpInst*)allocMemPool(sizeof(InterpInst) + sizeof(uint32_t) * dataLen);
    ins->opcode = opcode;
    return ins;
}

InterpInst* InterpCompiler::insertInsBB(InterpBasicBlock *bb, InterpInst *prevIns, int opcode)
{
    InterpInst *ins = newIns(opcode, getDataLen(opcode));

    ins->prev = prevIns;

    if (prevIns)
    {
        ins->next = prevIns->next;
        prevIns->next = ins;
    }
    else
    {
        ins->next = bb->firstIns;
        bb->firstIns = ins;
    }

    if (ins->next == NULL)
    {
        bb->lastIns = ins;
    }
    else
    {
        ins->next->prev = ins;
    }

    ins->ilOffset = -1;
    return ins;
}

// Inserts a new instruction after prevIns. prevIns must be in cbb
InterpInst* InterpCompiler::insertIns(InterpInst *prevIns, int opcode)
{
    return insertInsBB(cbb, prevIns, opcode);
}

InterpInst* InterpCompiler::firstIns(InterpBasicBlock *bb)
{
    InterpInst *ins = bb->firstIns;
    if (!ins || !insIsNop(ins))
        return ins;
    while (ins && insIsNop(ins))
        ins = ins->next;
    return ins;
}

InterpInst* InterpCompiler::nextIns(InterpInst *ins)
{
    ins = ins->next;
    while (ins && insIsNop(ins))
        ins = ins->next;
    return ins;
}

InterpInst* InterpCompiler::prevIns(InterpInst *ins)
{
    ins = ins->prev;
    while (ins && insIsNop(ins))
        ins = ins->prev;
    return ins;
}

void InterpCompiler::clearIns(InterpInst *ins)
{
    ins->opcode = INTOP_NOP;
}

bool InterpCompiler::insIsNop(InterpInst *ins)
{
    return ins->opcode == INTOP_NOP;
}

InterpBasicBlock* InterpCompiler::allocBB()
{
    InterpBasicBlock *bb = (InterpBasicBlock*)allocMemPool(sizeof(InterpBasicBlock));
    bb->ilOffset = -1;
    bb->nativeOffset = -1;
    bb->stackHeight = -1;
    bb->index = BBCount++;

    return bb;
}

InterpBasicBlock* InterpCompiler::getBB(uint32_t ilOffset)
{
    InterpBasicBlock *bb = offsetToBB [ilOffset];

    if (!bb)
    {
        bb = allocBB ();

        bb->ilOffset = ilOffset;
        offsetToBB[ilOffset] = bb;
    }

    return bb;
}

// FIXME Replace this
static inline uint32_t clzI4(uint32_t val)
{
    uint32_t count = 0;
    while (val) {
        val = val >> 1;
        count++;
    }
    return 32 - count;
}


int getBBLinksCapacity(int links)
{
    if(links <= 2)
        return links;
    // Return the next power of 2 bigger or equal to links
    uint32_t leadingZeroes = clzI4(links - 1);
    return 1 << (32 - leadingZeroes);
}


void InterpCompiler::linkBBs(InterpBasicBlock *from, InterpBasicBlock *to)
{
    int i;
    bool found = FALSE;

    for (i = 0; i < from->outCount; i++)
    {
        if (to == from->outBBs[i])
        {
            found = TRUE;
            break;
        }
    }
    if (!found)
    {
        int prevCapacity = getBBLinksCapacity(from->outCount);
        int newCapacity = getBBLinksCapacity(from->outCount + 1);
        if (newCapacity > prevCapacity)
        {
            InterpBasicBlock **newa = (InterpBasicBlock**)allocMemPool(newCapacity * sizeof(InterpBasicBlock*));
            memcpy(newa, from->outBBs, from->outCount * sizeof(InterpBasicBlock*));
            from->outBBs = newa;
        }
        from->outBBs [from->outCount] = to;
        from->outCount++;
    }

    found = FALSE;
    for (i = 0; i < to->inCount; i++)
    {
        if (from == to->inBBs [i])
        {
            found = TRUE;
            break;
        }
    }

    if (!found) {
        int prevCapacity = getBBLinksCapacity(to->inCount);
        int newCapacity = getBBLinksCapacity(to->inCount + 1);
        if (newCapacity > prevCapacity) {
            InterpBasicBlock **newa = (InterpBasicBlock**)allocMemPool(newCapacity * sizeof(InterpBasicBlock*));
            memcpy(newa, from->inBBs, from->inCount * sizeof(InterpBasicBlock*));
            from->inBBs = newa;
        }
        to->inBBs [to->inCount] = from;
        to->inCount++;
    }
}

// array must contain ref
static void removeBBRef(InterpBasicBlock **array, InterpBasicBlock *ref, int len)
{
    int i = 0;
    while(array[i] != ref)
    {
        i++;
    }
    i++;
    while(i < len)
    {
        array[i - 1] = array[i];
        i++;
    }
}

void InterpCompiler::unlinkBBs(InterpBasicBlock *from, InterpBasicBlock *to)
{
    removeBBRef(from->outBBs, to, from->outCount);
    from->outCount--;
    removeBBRef(to->inBBs, from, to->inCount);
    to->inCount--;
}

uint32_t InterpCompiler::createVarExplicit(InterpType mt, CORINFO_CLASS_HANDLE clsHnd, int size)
{
    if (varsSize == varsCapacity) {
        varsCapacity *= 2;
        if (varsCapacity == 0)
            varsCapacity = 16;
        vars = (InterpVar*) reallocTemporary(vars, varsCapacity * sizeof(InterpVar));
    }
    InterpVar *var = &vars[varsSize];

    var->mt = mt;
    var->clsHnd = clsHnd;
    var->size = size;
    var->indirects = 0;
    var->offset = -1;
    var->liveStart = -1;

    varsSize++;
    return varsSize - 1;
}

void ensureStack(int additional)
{
    uint32_t currentSize = stackPointer - stackBase;

    if (additional + currentSize > stackCapacity) {
        ptrdiff_t sppos = td->sp - td->stack;

    td->stack_capacity *= 2;
    td->stack = (StackInfo*) g_realloc (td->stack, td->stack_capacity * sizeof (td->stack [0]));
    td->sp = td->stack + sppos;

    }
    guint new_height = current_height + additional;
    if (new_height > td->stack_capacity)
        realloc_stack (td);

}

void pushTypeExplicit(StackType stackType, CORINFO_CLASS_HANDLE clsHnd, int size)
{

}
 
void pushType(StackType stackType, CORINFO_CLASS_HANDLE clsHnd)
{

}
 
void pushTypeVT(CORINFO_CLASS_HANDLE clsHnd, int size)
{

}


InterpCompiler::InterpCompiler(COMP_HANDLE compHnd,
                               CORINFO_METHOD_INFO* methodInfo)
{
    this->methodHnd = methodInfo->ftn;
    this->compHnd = compHnd;
    this->methodInfo = methodInfo;
}

int InterpCompiler::CompileMethod(InterpMethod **interpMethod)
{
    GenerateCode(methodInfo);
    return CORJIT_OK;
}

int InterpCompiler::GenerateCode(CORINFO_METHOD_INFO* methodInfo)
{
    uint8_t *code = methodInfo->ILCode;
    uint8_t *codeEnd = code + methodInfo->ILCodeSize;

    offsetToBB = (InterpBasicBlock**)allocMemPool(sizeof(InterpBasicBlock*) * (methodInfo->ILCodeSize + 1));
    stackCapacity = methodInfo->maxStack + 1;
    stackBase = stackPointer = (StackInfo*)allocMemPool(sizeof(StackInfo) * stackCapacity);

    while (code < codeEnd)
    {
        switch (*code)
        {
            case CEE_RET:
                break;
            default:
                break;
        }
    }

    return CORJIT_OK;
}
