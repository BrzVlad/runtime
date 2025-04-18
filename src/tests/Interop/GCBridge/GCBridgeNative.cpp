#include <platformdefines.h>

#include <stdio.h>

struct StronglyConnectedComponent final
{
    size_t Count;
    void** ContextMemory;
};

struct ComponentCrossReference final
{
    size_t SourceGroupIndex;
    size_t DestinationGroupIndex;
};

extern "C"
DLL_EXPORT int SimplePrint()
{
    printf ("Hello bitches\n");
    return 100;
}

typedef void (*MarkCrossReferencesFtn)(size_t, StronglyConnectedComponent*, size_t, ComponentCrossReference*);

static void MarkCrossReferences(
    size_t sccsLen,
    StronglyConnectedComponent* sccs,
    size_t ccrsLen,
    ComponentCrossReference* ccrs)
{
    printf ("MarkCrossReferences, yupii\n");
}

extern "C"
DLL_EXPORT MarkCrossReferencesFtn GetMarkCrossReferencesFtn()
{
    return MarkCrossReferences;
}

