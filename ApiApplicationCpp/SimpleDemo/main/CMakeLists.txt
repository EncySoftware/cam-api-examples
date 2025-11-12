cmake_minimum_required(VERSION 4.0)
project(SimpleDemo)

set(CMAKE_RUNTIME_OUTPUT_DIRECTORY_DEBUG   "C:/Program Files/ENCY Software/ENCY NB 2/Bin64")
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY_RELEASE "C:/Program Files/ENCY Software/ENCY NB 2/Bin64")

set(CMAKE_CXX_STANDARD 20)

include_directories("${CMAKE_SOURCE_DIR}/../../../SDK/tlb")
add_executable(CamRunner main.cpp
        NativeLibLoader.cpp
        NativeLibLoader.h
        PathAliases.cpp
        PathAliases.h
        StarterNativeUtils.cpp
        StarterNativeUtils.h
        CamApplication.cpp
        CamApplication.h
        CamApi.SDK.h
)