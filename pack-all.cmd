@echo off
cd /D %~dp0

SET nopause=true

call :PACKPROJECT "Attributes\ExtensionAttributesManageNet\commands\pack.cmd"
call :PACKPROJECT "CLData\ExtensionGeomCLDataConverterNet\commands\pack.cmd"
@REM call :PACKPROJECT "ExtensionEmpty\ExtensionEmptyCpp\commands\pack.cmd"
@REM call :PACKPROJECT "ExtensionEmpty\ExtensionEmptyDelphi\commands\pack.cmd"
call :PACKPROJECT "ExtensionEmpty\ExtensionEmptyNet\commands\pack.cmd"
call :PACKPROJECT "ExtensionGlobal\ExtensionGlobalNet\commands\pack.cmd"
call :PACKPROJECT "ExtensionOperationPopup\ExtensionOperationPopupNet\commands\pack.cmd"
call :PACKPROJECT "ExtensionOperationPopup\ExtensionOperationPopupOnChangeNet\commands\pack.cmd"
@REM call :PACKPROJECT "ExtensionUtility\ExtensionUtilityCpp\commands\pack.cmd"
@REM call :PACKPROJECT "ExtensionUtility\ExtensionUtilityDelphi\commands\pack.cmd"
call :PACKPROJECT "ExtensionUtility\ExtensionUtilityNet\commands\pack.cmd"
call :PACKPROJECT "FullWorkflow\FullWorkflow3DProject\commands\pack.cmd"
call :PACKPROJECT "GCodeGeneration\ExtensionUtilityNCMakerNet\commands\pack.cmd"
call :PACKPROJECT "Geometry\AddinImportObjNet\commands\pack.cmd"
call :PACKPROJECT "Geometry\AddinImportSvgNet\commands\pack.cmd"
call :PACKPROJECT "Geometry\ExtensionUtilityGeomCustomImportNet\commands\pack.cmd"
call :PACKPROJECT "Geometry\ExtensionUtilityGeometryImporterNet\commands\pack.cmd"
call :PACKPROJECT "Geometry\ExtensionUtilityGeometryModelNet\commands\pack.cmd"
call :PACKPROJECT "Geometry\ExtensionUtilityGeometryPickerNet\commands\pack.cmd"
call :PACKPROJECT "Geometry\ExtensionUtilityImportSvgNet\commands\pack.cmd"
call :PACKPROJECT "Geometry\ExtensionUtilityLCSCreatorNet\commands\pack.cmd"
call :PACKPROJECT "Geometry\UtilityGeometryEntityReader\commands\pack.cmd"
call :PACKPROJECT "GeometryModelNodePopup\NodeFullNameAlert\commands\pack.cmd"
call :PACKPROJECT "Operation\ExtensionOperationParamsNet\commands\pack.cmd"
call :PACKPROJECT "Operation\ExtensionOperationSimpleNet\commands\pack.cmd"
@REM call :PACKPROJECT "PLMIntegration\PLMExtensionDelphi\commands\pack.cmd"
call :PACKPROJECT "PLMIntegration\PLMExtensionNet\commands\pack.cmd"
call :PACKPROJECT "Project\ExtensionUtilityExportInformationNet\commands\pack.cmd"
call :PACKPROJECT "ProjectMachine\ExtensionUtilityProjectMachineInfoNet\commands\pack.cmd"
call :PACKPROJECT "ProjectMachine\MachinePropsChangeNet\commands\pack.cmd"
call :PACKPROJECT "ProjectToolsList\ExtensionUtilityProjectToolsListNet\commands\pack.cmd"
call :PACKPROJECT "Technologist\Operation\CreateUserOperationNet\commands\pack.cmd"
call :PACKPROJECT "Technologist\Operation\FlippingToolNet\commands\pack.cmd"
call :PACKPROJECT "Technologist\Operation\OperationNet\commands\pack.cmd"
call :PACKPROJECT "Technologist\Operation\OperationToolAddNet\commands\pack.cmd"
call :PACKPROJECT "Technologist\Operation\RenameOperationsNet\commands\pack.cmd"
call :PACKPROJECT "Technologist\SetFeedZone\commands\pack.cmd"
call :PACKPROJECT "UI\ExtensionUtilityDialogWindowNet\commands\pack.cmd"
call :PACKPROJECT "UI\ExtensionUtilityMessageBoxNet\commands\pack.cmd"
call :PACKPROJECT "UI\ExtensionUtilityNotifyNet\commands\pack.cmd"

pause

EXIT /B %EXIT_CODE%

:: ========== FUNCTIONS ==========

:PACKPROJECT
    SET CMDFILE=%~1
    echo.
    echo.
    echo.
    echo =============================================================
    echo Packing: "%CMDFILE%"
    echo =============================================================

    cmd /c "SETLOCAL & cd /D "%~dp0" & call "%CMDFILE%""

EXIT /B   