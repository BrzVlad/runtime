set(CMAKE_INCLUDE_CURRENT_DIR ON)
set(INTERPRETER_SOURCES
  compiler.cpp
  ee_il_dll.cpp)

  set(INTERPRETER_LINK_LIBRARIES
)

if(CLR_CMAKE_TARGET_WIN32)
    list(APPEND INTERPRETER_LINK_LIBRARIES
        ${STATIC_MT_CRT_LIB}
        ${STATIC_MT_VCRT_LIB}
        kernel32.lib
        advapi32.lib
        ole32.lib
        oleaut32.lib
        uuid.lib
        user32.lib
        version.lib
        shlwapi.lib
        bcrypt.lib
        RuntimeObject.lib
        delayimp.lib
    )
endif(CLR_CMAKE_TARGET_WIN32)

if(CLR_CMAKE_TARGET_WIN32)
    set(CLRINTERPRETER_EXPORTS ${CMAKE_CURRENT_LIST_DIR}/clrinterpreter.exports)
    set(EXPORTS_FILE ${CMAKE_CURRENT_BINARY_DIR}/clrinterpreter.def)
else()
    set(CLRINTERPRETER_EXPORTS ${CMAKE_CURRENT_LIST_DIR}/clrinterpreter_unixexports.src)
    set(EXPORTS_FILE ${CMAKE_CURRENT_BINARY_DIR}/clrinterpreter.exports)
endif()

generate_exports_file(${CLRINTERPRETER_EXPORTS} ${EXPORTS_FILE})

add_library_clr(clrinterpreter SHARED ${INTERPRETER_SOURCES})
target_link_libraries(clrinterpreter
        PRIVATE
        ${INTERPRETER_LINK_LIBRARIES}
    )

add_custom_target(interpreter_exports DEPENDS ${EXPORTS_FILE})
add_dependencies(clrinterpreter interpreter_exports)

set_exports_linker_option(${EXPORTS_FILE})
set_property(TARGET clrinterpreter APPEND_STRING PROPERTY LINK_FLAGS ${EXPORTS_LINKER_OPTION})

install_clr(TARGETS clrinterpreter DESTINATIONS . COMPONENT runtime)
