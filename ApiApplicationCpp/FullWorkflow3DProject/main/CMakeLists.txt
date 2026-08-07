cmake_minimum_required(VERSION 4.0)
project(FullWorkflow3DProject)

set(CMAKE_RUNTIME_OUTPUT_DIRECTORY_DEBUG   "C:/Program Files/ENCY Software/ENCY 2/Bin64")
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY_RELEASE "C:/Program Files/ENCY Software/ENCY 2/Bin64")

set(CMAKE_CXX_STANDARD 20)

include_directories("${CMAKE_SOURCE_DIR}/../../../SDK/tlb")
add_executable(FullWorkflow3DProject main.cpp
        pch.cpp
        pch.h
        NativeLibLoader.cpp
        NativeLibLoader.h
        PathAliases.cpp
        PathAliases.h
        StarterNativeUtils.cpp
        StarterNativeUtils.h
        CamApplication.cpp
        CamApplication.h
        CamApi.SDK.h
        Utils.cpp
        Utils.h
        Helpers/ModelHelper.cpp
        Helpers/ModelHelper.h
        Helpers/ProjectHelper.cpp
        Helpers/ProjectHelper.h
        Helpers/TechnologyHelper.cpp
        Helpers/TechnologyHelper.h
        Helpers/SimulationHelper.cpp
        Helpers/SimulationHelper.h
)
target_include_directories(FullWorkflow3DProject PRIVATE
        ${CMAKE_CURRENT_SOURCE_DIR}
)