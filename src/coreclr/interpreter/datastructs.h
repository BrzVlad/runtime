// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _DATASTRUCTS_H_
#define _DATASTRUCTS_H_

template <typename T>
class PtrArray
{
private:
    int32_t m_size, m_capacity;
    T *m_array;

    void    Grow();
public:
    PtrArray();
    ~PtrArray();

    int32_t GetSize();
    void    Add(T element);
    T       Get(int32_t index);
};

#endif
