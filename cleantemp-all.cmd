@echo off
cd /D %~dp0

SET nopause=true

call :CLEANTEMPPROJECT "ApiApplicationNet\SimpleDemo\commands\build.cmd"
call :CLEANTEMPPROJECT "ApplicationNet\CreateOperations\commands\build.cmd"
call :CLEANTEMPPROJECT "ApplicationNet\FullWorkflow3DProject\commands\build.cmd"
call :CLEANTEMPPROJECT "ApplicationNet\GeometryImporter\commands\build.cmd"
call :CLEANTEMPPROJECT "Attributes\ExtensionAttributesManageNet\commands\cleantemp.cmd"
call :CLEANTEMPPROJECT "CLData\ExtensionGeomCLDataConverterNet\commands\cleantemp.cmd"
call :CLEANTEMPPROJECT "ExtensionEmpty\ExtensionEmptyCpp\commands\cleantemp.cmd"
@REM call :CLEANTEMPPROJECT "ExtensionEmpty\ExtensionEmptyDelphi\commands\cleantemp.cmd"
@REM call :CLEANTEMPPROJECT "ExtensionEmpty\ExtensionEmptyNet\commands\cleantemp.cmd"
call :CLEANTEMPPROJECT "ExtensionGlobal\ExtensionGlobalNet\commands\cleantemp.cmd"
call :CLEANTEMPPROJECT "ExtensionOperationPopup\ExtensionOperationPopupNet\commands\cleantemp.cmd"
call :CLEANTEMPPROJECT "ExtensionOperationPopup\ExtensionOperationPopupOnChangeNet\commands\cleantemp.cmd"
call :CLEANTEMPPROJECT "ExtensionUtility\ExtensionUtilityCpp\commands\cleantemp.cmd"
call :CLEANTEMPPROJECT "ExtensionUtility\ExtensionUtilityDelphi\commands\cleantemp.cmd"
call :CLEANTEMPPROJECT "ExtensionUtility\ExtensionUtilityNet\commands\cleantemp.cmd"
call :CLEANTEMPPROJECT "FullWorkflow\FullWorkflow3DProject\commands\build.cmd"
call :CLEANTEMPPROJECT "GCodeGeneration\ExtensionUtilityNCMakerNet\commands\cleantemp.cmd"
call :CLEANTEMPPROJECT "Geometry\AddinImportObjNet\commands\cleantemp.cmd"
call :CLEANTEMPPROJECT "Geometry\AddinImportSvgNet\commands\cleantemp.cmd"
call :CLEANTEMPPROJECT "Geometry\ExtensionUtilityGeomCustomImportNet\commands\cleantemp.cmd"
call :CLEANTEMPPROJECT "Geometry\ExtensionUtilityGeometryImporterNet\commands\cleantemp.cmd"
call :CLEANTEMPPROJECT "Geometry\ExtensionUtilityGeometryModelNet\commands\cleantemp.cmd"
call :CLEANTEMPPROJECT "Geometry\ExtensionUtilityGeometryPickerNet\commands\cleantemp.cmd"
call :CLEANTEMPPROJECT "Geometry\ExtensionUtilityImportSvgNet\commands\cleantemp.cmd"
call :CLEANTEMPPROJECT "Geometry\ExtensionUtilityLCSCreatorNet\commands\build.cmd"
call :CLEANTEMPPROJECT "Geometry\UtilityGeometryEntityReader\commands\build.cmd"
@REM call :CLEANTEMPPROJECT "MachiningTools\DIN4000ImportPluginNet\commands\cleantemp.cmd"
@REM call :CLEANTEMPPROJECT "MachiningTools\MachiningToolsCreateExampleNet\commands\cleantemp.cmd"
call :CLEANTEMPPROJECT "Operation\ExtensionOperationParamsNet\commands\cleantemp.cmd"
call :CLEANTEMPPROJECT "Operation\ExtensionOperationSimpleNet\commands\cleantemp.cmd"
call :CLEANTEMPPROJECT "PLMIntegration\PLMExtensionDelphi\commands\cleantemp.cmd"
call :CLEANTEMPPROJECT "PLMIntegration\PLMExtensionNet\commands\cleantemp.cmd"
call :CLEANTEMPPROJECT "ProjectMachine\ExtensionUtilityProjectMachineInfoNet\commands\cleantemp.cmd"
call :CLEANTEMPPROJECT "ProjectMachine\MachinePropsChangeNet\commands\build.cmd"
call :CLEANTEMPPROJECT "ProjectToolsList\ExtensionUtilityProjectToolsListNet\commands\cleantemp.cmd"
call :CLEANTEMPPROJECT "Technologist\Operation\FlippingToolNet\commands\build.cmd"
call :CLEANTEMPPROJECT "Technologist\Operation\OperationNet\commands\build.cmd"
call :CLEANTEMPPROJECT "Technologist\Operation\OperationToolAddNet\commands\build.cmd"
call :CLEANTEMPPROJECT "Technologist\Operation\RenameOperationsNet\commands\build.cmd"
call :CLEANTEMPPROJECT "UI\ExtensionUtilityDialogWindowNet\commands\cleantemp.cmd"
call :CLEANTEMPPROJECT "UI\ExtensionUtilityMessageBoxNet\commands\cleantemp.cmd"
call :CLEANTEMPPROJECT "UI\ExtensionUtilityNotifyNet\commands\cleantemp.cmd"

pause

EXIT /B %EXIT_CODE%

:: ========== FUNCTIONS ==========

:CLEANTEMPPROJECT
    SET CMDFILE=%~1
    echo.
    echo.
    echo.
    echo =============================================================
    echo Cleaning: "%CMDFILE%"
    echo =============================================================

    cmd /c "SETLOCAL & cd /D "%~dp0" & call "%CMDFILE%""

EXIT /B   