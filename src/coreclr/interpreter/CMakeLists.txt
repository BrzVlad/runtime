set(CMAKE_INCLUDE_CURRENT_DIR ON)
set(INTERPRETER_SOURCES
  compiler.cpp
  ee_il_dll.cpp)
set(INTERPRETER_LINK_LIBRARIES
#    utilcodestaticnohost
#    coreclrminipal
#    mscorrc
#    coreclrpal
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
else()
    list(APPEND INTERPRETER_LINK_LIBRARIES
        # ${START_WHOLE_ARCHIVE} # force all PAL objects to be included so all exports are available
        # coreclrpal
        # ${END_WHOLE_ARCHIVE}
        # mscorrc
    )
endif(CLR_CMAKE_TARGET_WIN32)

if(CLR_CMAKE_TARGET_WIN32)
  set(CLRINTERPRETER_EXPORTS ${CMAKE_CURRENT_LIST_DIR}/clrinterpreter.exports)
  set(INTERPRETER_EXPORTS_FILE ${CMAKE_CURRENT_BINARY_DIR}/clrinterpreter.exports.def)
  preprocess_file (${CLRINTERPRETER_EXPORTS} ${INTERPRETER_EXPORTS_FILE})

  set(INTERPRETER_DEF_FILE ${INTERPRETER_EXPORTS_FILE})
else()
#  set(CLRINTERPRETER_EXPORTS ${CMAKE_CURRENT_LIST_DIR}/ClrJit.PAL.exports)

#  set(INTERPRETER_EXPORTS_FILE ${CMAKE_CURRENT_BINARY_DIR}/clrjit.exports)
#  generate_exports_file(${CLRINTERPRETER_EXPORTS} ${INTERPRETER_EXPORTS_FILE})

#  set_exports_linker_option(${JIT_EXPORTS_FILE})
#  set(JIT_EXPORTS_LINKER_OPTION ${EXPORTS_LINKER_OPTION})
endif()

add_library_clr(clrinterpreter SHARED ${INTERPRETER_SOURCES} ${INTERPRETER_DEF_FILE})
target_link_libraries(clrinterpreter
        PRIVATE
        ${INTERPRETER_LINK_LIBRARIES}
    )


install_clr(TARGETS clrinterpreter DESTINATIONS . COMPONENT runtime)