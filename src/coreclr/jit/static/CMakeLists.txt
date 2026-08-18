project(ClrJit)

set_source_files_properties(${JIT_EXPORTS_FILE} PROPERTIES GENERATED TRUE)

set(JIT_STATIC_SOURCES ${JIT_SOURCES})
set(JIT_STATIC_ARCH_SOURCES ${JIT_ARCH_SOURCES})

if(CLR_CMAKE_HOST_ARCH_WASM)
    set(JIT_STATIC_ARCH_SOURCES
        ${JIT_SOURCE_DIR}/emitwasm.cpp
        ${JIT_SOURCE_DIR}/hwintrinsicwasm.cpp
        ${JIT_SOURCE_DIR}/registeropswasm.cpp
        ${JIT_SOURCE_DIR}/simd.cpp
        ${JIT_SOURCE_DIR}/targetwasm.cpp
    )
endif()

add_library_clr(clrjit_static
    OBJECT
    ${JIT_STATIC_SOURCES}
    ${JIT_STATIC_ARCH_SOURCES}
    ${JITSHARED_SOURCES}
)

if(CLR_CMAKE_HOST_UNIX)
    add_dependencies(clrjit_static coreclrpal gcinfo)
endif(CLR_CMAKE_HOST_UNIX)

target_include_directories(clrjit_static PRIVATE ${JIT_SOURCE_DIR})
target_precompile_headers(clrjit_static PRIVATE [["jitpch.h"]])

if(CLR_CMAKE_HOST_ARCH_WASM)
    target_compile_definitions(clrjit_static PRIVATE INTERPRETER_ONLY_JIT RUNTIME_ONLY_JIT)
    target_compile_options(clrjit_static PRIVATE -Oz -flto -fomit-frame-pointer -fno-stack-protector)
endif()
