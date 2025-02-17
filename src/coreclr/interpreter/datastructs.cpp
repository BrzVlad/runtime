// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdlib.h>
#include <assert.h>
#include "datastructs.h"

template <typename T>
PtrArray<T>::PtrArray()
{
    m_size = 0;
    m_capacity = 0;
    m_array = NULL;
}

template <typename T>
PtrArray<T>::~PtrArray()
{
    if (m_capacity > 0)
        free(m_array);
}

template <typename T>
int32_t PtrArray<T>::GetSize()
{
    return m_size;
}

template <typename T>
void PtrArray<T>::Grow()
{
    if (m_capacity)
        m_capacity *= 2;
    else
        m_capacity = 16;

    m_array = realloc(m_array, m_capacity * sizeof(T));
}

template <typename T>
void PtrArray<T>::Add(T element)
{
   if (m_size == m_capacity)
        Grow();
    m_array[m_size] = element;
    m_size++;
}

template <typename T>
T PtrArray<T>::Get(int32_t index)
{
    assert(index < m_size);
    return m_array[index];
}
