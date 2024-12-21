set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(INTERP_SOURCES
  compiler.cpp
  ee_il_dll.cpp)

set(INTERP_LINK_LIBRARIES
   utilcodestaticnohost
   coreclrminipal
   mscorrc
   coreclrpal
)


add_library_clr(clrinterp SHARED ${INTERP_SOURCES})

target_link_libraries(clrinterp
        PRIVATE
        ${INTERP_LINK_LIBRARIES}
    )

install_clr(TARGETS clrinterp DESTINATIONS . COMPONENT runtime)
