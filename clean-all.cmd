@echo off
cd /D %~dp0

SET nopause=true

call :CLEANPROJECT "ApiApplicationNet\SimpleDemo\commands\build.cmd"
call :CLEANPROJECT "ApplicationNet\CreateOperations\commands\build.cmd"
call :CLEANPROJECT "ApplicationNet\FullWorkflow3DProject\commands\build.cmd"
call :CLEANPROJECT "ApplicationNet\GeometryImporter\commands\build.cmd"
call :CLEANPROJECT "Attributes\ExtensionAttributesManageNet\commands\clean.cmd"
call :CLEANPROJECT "CLData\ExtensionGeomCLDataConverterNet\commands\clean.cmd"
call :CLEANPROJECT "ExtensionEmpty\ExtensionEmptyCpp\commands\clean.cmd"
call :CLEANPROJECT "ExtensionEmpty\ExtensionEmptyDelphi\commands\clean.cmd"
call :CLEANPROJECT "ExtensionEmpty\ExtensionEmptyNet\commands\clean.cmd"
call :CLEANPROJECT "ExtensionGlobal\ExtensionGlobalNet\commands\clean.cmd"
call :CLEANPROJECT "ExtensionOperationPopup\ExtensionOperationPopupNet\commands\clean.cmd"
call :CLEANPROJECT "ExtensionOperationPopup\ExtensionOperationPopupOnChangeNet\commands\clean.cmd"
call :CLEANPROJECT "ExtensionUtility\ExtensionUtilityCpp\commands\clean.cmd"
call :CLEANPROJECT "ExtensionUtility\ExtensionUtilityDelphi\commands\clean.cmd"
call :CLEANPROJECT "ExtensionUtility\ExtensionUtilityNet\commands\clean.cmd"
call :CLEANPROJECT "FullWorkflow\FullWorkflow3DProject\commands\build.cmd"
call :CLEANPROJECT "GCodeGeneration\ExtensionUtilityNCMakerNet\commands\clean.cmd"
call :CLEANPROJECT "Geometry\AddinImportObjNet\commands\clean.cmd"
call :CLEANPROJECT "Geometry\AddinImportSvgNet\commands\clean.cmd"
call :CLEANPROJECT "Geometry\ExtensionUtilityGeomCustomImportNet\commands\clean.cmd"
call :CLEANPROJECT "Geometry\ExtensionUtilityGeometryImporterNet\commands\clean.cmd"
call :CLEANPROJECT "Geometry\ExtensionUtilityGeometryModelNet\commands\clean.cmd"
call :CLEANPROJECT "Geometry\ExtensionUtilityGeometryPickerNet\commands\clean.cmd"
call :CLEANPROJECT "Geometry\ExtensionUtilityGeomPickerNet\commands\clean.cmd"
call :CLEANPROJECT "Geometry\ExtensionUtilityImportSvgNet\commands\clean.cmd"
call :CLEANPROJECT "Geometry\ExtensionUtilityLCSCreatorNet\commands\build.cmd"
call :CLEANPROJECT "Geometry\UtilityGeometryEntityReader\commands\build.cmd"
@REM call :CLEANPROJECT "MachiningTools\DIN4000ImportPluginNet\commands\clean.cmd"
@REM call :CLEANPROJECT "MachiningTools\MachiningToolsCreateExampleNet\commands\clean.cmd"
call :CLEANPROJECT "Operation\ExtensionOperationParamsNet\commands\clean.cmd"
call :CLEANPROJECT "Operation\ExtensionOperationSimpleNet\commands\clean.cmd"
call :CLEANPROJECT "PLMIntegration\PLMExtensionDelphi\commands\clean.cmd"
call :CLEANPROJECT "PLMIntegration\PLMExtensionNet\commands\clean.cmd"
call :CLEANPROJECT "ProjectMachine\ExtensionUtilityProjectMachineInfoNet\commands\clean.cmd"
call :CLEANPROJECT "ProjectMachine\MachinePropsChangeNet\commands\build.cmd"
call :CLEANPROJECT "ProjectToolsList\ExtensionUtilityProjectToolsListNet\commands\clean.cmd"
call :CLEANPROJECT "Technologist\Operation\FlippingToolNet\commands\build.cmd"
call :CLEANPROJECT "Technologist\Operation\OperationNet\commands\build.cmd"
call :CLEANPROJECT "Technologist\Operation\OperationToolAddNet\commands\build.cmd"
call :CLEANPROJECT "Technologist\Operation\RenameOperationsNet\commands\build.cmd"
call :CLEANPROJECT "UI\ExtensionUtilityDialogWindowNet\commands\clean.cmd"
call :CLEANPROJECT "UI\ExtensionUtilityMessageBoxNet\commands\clean.cmd"
call :CLEANPROJECT "UI\ExtensionUtilityNotifyNet\commands\clean.cmd"

pause

EXIT /B %EXIT_CODE%

:: ========== FUNCTIONS ==========

:CLEANPROJECT
    SET CMDFILE=%~1
    echo.
    echo.
    echo.
    echo =============================================================
    echo Cleaning: "%CMDFILE%"
    echo =============================================================

    cmd /c "SETLOCAL & cd /D "%~dp0" & call "%CMDFILE%""

EXIT /B   