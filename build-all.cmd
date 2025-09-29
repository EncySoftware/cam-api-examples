@echo off
cd /D %~dp0

SET nopause=true

call :BUILDPROJECT "ApplicationEmpty\ApplicationEmptyNet\commands\build.cmd"
call :BUILDPROJECT "ApplicationSgfCreator\ApplicationSgfCreatorNet\commands\build.cmd"
call :BUILDPROJECT "Attributes\ExtensionAttributesManageNet\commands\build.cmd"
call :BUILDPROJECT "CLData\ExtensionGeomCLDataConverterNet\commands\build.cmd"
@REM call :BUILDPROJECT "ExtensionEmpty\ExtensionEmptyCpp\commands\build.cmd"
@REM call :BUILDPROJECT "ExtensionEmpty\ExtensionEmptyDelphi\commands\build.cmd"
call :BUILDPROJECT "ExtensionEmpty\ExtensionEmptyNet\commands\build.cmd"
call :BUILDPROJECT "ExtensionGlobal\ExtensionGlobalNet\commands\build.cmd"
call :BUILDPROJECT "ExtensionOperationPopup\ExtensionOperationPopupNet\commands\build.cmd"
call :BUILDPROJECT "ExtensionOperationPopup\ExtensionOperationPopupOnChangeNet\commands\build.cmd"
@REM call :BUILDPROJECT "ExtensionUtility\ExtensionUtilityCpp\commands\build.cmd"
@REM call :BUILDPROJECT "ExtensionUtility\ExtensionUtilityDelphi\commands\build.cmd"
call :BUILDPROJECT "ExtensionUtility\ExtensionUtilityNet\commands\build.cmd"
call :BUILDPROJECT "GCodeGeneration\ExtensionUtilityNCMakerNet\commands\build.cmd"
call :BUILDPROJECT "Geometry\AddinImportObjNet\commands\build.cmd"
call :BUILDPROJECT "Geometry\AddinImportSvgNet\commands\build.cmd"
call :BUILDPROJECT "Geometry\ExtensionUtilityGeomCustomImportNet\commands\build.cmd"
call :BUILDPROJECT "Geometry\ExtensionUtilityGeometryImporterNet\commands\build.cmd"
call :BUILDPROJECT "Geometry\ExtensionUtilityGeometryModelNet\commands\build.cmd"
call :BUILDPROJECT "Geometry\ExtensionUtilityGeometryPickerNet\commands\build.cmd"
call :BUILDPROJECT "Geometry\ExtensionUtilityGeomPickerNet\commands\build.cmd"
call :BUILDPROJECT "Geometry\ExtensionUtilityImportSvgNet\commands\build.cmd"
call :BUILDPROJECT "MachiningTools\DIN4000ImportPluginNet\commands\build.cmd"
call :BUILDPROJECT "MachiningTools\MachiningToolsCreateExampleNet\commands\build.cmd"
call :BUILDPROJECT "Operation\ExtensionOperationParamsNet\commands\build.cmd"
call :BUILDPROJECT "Operation\ExtensionOperationSimpleNet\commands\build.cmd"
@REM call :BUILDPROJECT "PLMIntegration\PLMExtensionDelphi\commands\build.cmd"
call :BUILDPROJECT "PLMIntegration\PLMExtensionNet\commands\build.cmd"
call :BUILDPROJECT "ProjectMachine\ExtensionUtilityProjectMachineInfoNet\commands\build.cmd"
call :BUILDPROJECT "ProjectToolsList\ExtensionUtilityProjectToolsListNet\commands\build.cmd"
call :BUILDPROJECT "UI\ExtensionUtilityDialogWindowNet\commands\build.cmd"
call :BUILDPROJECT "UI\ExtensionUtilityMessageBoxNet\commands\build.cmd"
call :BUILDPROJECT "UI\ExtensionUtilityNotifyNet\commands\build.cmd"

pause

EXIT /B %EXIT_CODE%

:: ========== FUNCTIONS ==========

:BUILDPROJECT
    SET CMDFILE=%~1
    echo.
    echo.
    echo.
    echo =============================================================
    echo Building: "%CMDFILE%"
    echo =============================================================

    cmd /c "SETLOCAL & cd /D "%~dp0" & call "%CMDFILE%""

EXIT /B   