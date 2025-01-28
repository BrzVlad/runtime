// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _COMPILER_H_
#define _COMPILER_H_

#include "intops.h"

// Types that can exist on the IL execution stack. They are used only during
// IL import compilation stage.
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
    InterpTypeVOID
};

struct InterpInst
{
    uint32_t opcode;
    InterpInst *next, *prev;
    int ilOffset;
    uint32_t flags;
    uint32_t dVar;
    uint32_t sVars[3]; // Currently all instructions have at most 3 sregs

    uint32_t data[];
};

struct InterpBasicBlock
{
    int index;
    int32_t ilOffset, nativeOffset;
    int stackHeight;

    InterpInst *firstIns, *lastIns;
    InterpBasicBlock *nextBB;

    int inCount;
    InterpBasicBlock **inBBs;
    int outCount;
    InterpBasicBlock **outBBs;

};

struct InterpVar
{
    CORINFO_CLASS_HANDLE clsHnd;
    InterpType mt;
    int indirects;
    int offset;
    int size;
    // live_start and live_end are used by the offset allocator
    int liveStart;
    int liveEnd;
};

struct StackInfo
{
    CORINFO_CLASS_HANDLE clsHnd;
    StackType type;

    // The var associated with the value of this stack entry. Every time we push on
    // the stack a new var is created.
    int var;
};

typedef class ICorJitInfo* COMP_HANDLE;

class InterpMethod
{
public:
    CORINFO_METHOD_HANDLE methodHnd;
};

class InterpCompiler
{
private:
    CORINFO_METHOD_HANDLE methodHnd;
    COMP_HANDLE compHnd;
    CORINFO_METHOD_INFO* methodInfo;

    int GenerateCode(CORINFO_METHOD_INFO* methodInfo);

    void* allocMemPool(size_t numBytes);
    void* allocTemporary(size_t numBytes);
    void* reallocTemporary(void* ptr, size_t numBytes);
    void  freeTemporary(void* ptr);

    // Instructions
    InterpBasicBlock *cbb;

    bool        insIsNop(InterpInst *ins);
    InterpInst* addIns(int opcode);
    InterpInst* newIns(int opcode, int len);
    InterpInst* addInsExplicit(int opcode, int dataLen);
    InterpInst* insertInsBB(InterpBasicBlock *bb, InterpInst *prevIns, int opcode);
    InterpInst* insertIns(InterpInst *prevIns, int opcode);
    InterpInst* firstIns(InterpBasicBlock *bb);
    InterpInst* nextIns(InterpInst *ins);
    InterpInst* prevIns(InterpInst *ins);
    void        clearIns(InterpInst *ins);

    // Basic blocks
    int BBCount;
    InterpBasicBlock**  offsetToBB;

    InterpBasicBlock*   allocBB();
    InterpBasicBlock*   getBB(uint32_t ilOffset);
    void                linkBBs(InterpBasicBlock *from, InterpBasicBlock *to);
    void                unlinkBBs(InterpBasicBlock *from, InterpBasicBlock *to);

    // Vars
    InterpVar *vars;
    uint32_t varsSize;
    uint32_t varsCapacity;

    uint32_t createVarExplicit(InterpType mt, CORINFO_CLASS_HANDLE clsHnd, int size);

    // Stack
    StackInfo *stackPointer, *stackBase;
    uint32_t stackCapacity;

    void ensureStack(int additional);
    void pushTypeExplicit(StackType stackType, CORINFO_CLASS_HANDLE clsHnd, int size);
    void pushType(StackType stackType, CORINFO_CLASS_HANDLE clsHnd);
    void pushTypeVT(CORINFO_CLASS_HANDLE clsHnd, int size);

public:

    InterpCompiler(COMP_HANDLE compHnd, CORINFO_METHOD_INFO* methodInfo);

    int CompileMethod(InterpMethod **interpMethod);

};

#endif //_COMPILER_H_
