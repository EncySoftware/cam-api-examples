#include "pch.h"
#include "../Utils.h"
#include "TechnologyHelper.h"

#include <filesystem>
#include <random>
#include <comutil.h>
#include <shellapi.h>
#include <iostream>
#include <vector>

namespace fs = std::filesystem;

HRESULT SetupWorkpiece(ICamApiTechnologist* technologist) {
    HRESULT result = S_OK;

    ICamApiPartAndStageList* psl = nullptr;
    ICamApiPartStage* partStage = nullptr;
    ICamApiWorkpieceSetup* workpieceSetup = nullptr;
    ICamApiWorkpieceCoordinateSystem* workpieceCoordinateSystem = nullptr;
    try {
        // setup workpiece setup
        HRESULT hr = technologist->get_PartAndStageList(&psl);
        if (FAILED(hr) || !psl)
            throw std::runtime_error("get_PartAndStageList failed.");

        hr = psl->GetPartStage(0, 0, &partStage);
        if (FAILED(hr) || !partStage)
            throw std::runtime_error("GetPartStage failed.");

        hr = partStage->get_WorkpieceSetup(&workpieceSetup);
        if (FAILED(hr) || !workpieceSetup)
            throw std::runtime_error("get_WorkpieceSetup failed.");

        TST3DMatrix offset = {};
        offset.vX = { 1.0, 0.0, 0.0 };
        offset.vY = { 0.0, 1.0, 0.0 };
        offset.vZ = { 0.0, 0.0, 1.0 };
        offset.vT = { 0.0, 0.0, 100.0 };
        hr = workpieceSetup->put_Offset(offset);
        if (FAILED(hr))
            throw std::runtime_error("put_Offset failed.");

        // setup workpiece coordinate system
        hr = partStage->get_WorkpieceCoordinateSystem(&workpieceCoordinateSystem);
        if (FAILED(hr) || !workpieceCoordinateSystem)
            throw std::runtime_error("get_WorkpieceCoordinateSystem failed.");
        TST3DPoint offsetPoint = {};
        offsetPoint.X = -100.0;
        offsetPoint.Y = -60.0;
        offsetPoint.Z = 0.0;
        hr = workpieceCoordinateSystem->put_Offset(offsetPoint);
        if (FAILED(hr))
            throw std::runtime_error("put_Offset failed.");

        std::cout << "Workpiece setup completed." << std::endl;
    }
    catch (const std::exception& ex) {
        std::cerr << "Error in SetupStage(): " << ex.what() << std::endl;
        result = E_FAIL;
    }

    // clean
    if (workpieceCoordinateSystem)
        workpieceCoordinateSystem->Release();
    if (workpieceSetup)
        workpieceSetup->Release();
    if (partStage)
        partStage->Release();
    if (psl)
        psl->Release();

    // return
    return result;
}

void SetupOperationTool(ICamApiProject* project,
    BSTR operationId,
    BSTR toolId) {
    TResultStatus resultStatus = {};

    HRESULT hr = project->SetOperationTool(operationId, toolId, &resultStatus);
    if (resultStatus.Code == rsError)
        throw std::runtime_error("SetOperationTool failed: " + Utils::BSTRToString(resultStatus.Description));
    if (FAILED(hr))
        throw std::runtime_error("SetOperationTool failed.");
}

ICamApiTechOperation* CreateOperationObject(ICamApiTechnologist* technologist,
    BSTR prevOperationId,
    BSTR typeId) {
    TResultStatus resultStatus = {};
    ICamApiTechOperation* result = nullptr;

    try {
        // create operation
        _bstr_t bstrEmptyPrototypeId(L"");
        HRESULT hr = technologist->CreateOperation(typeId,
            prevOperationId,
            bstrEmptyPrototypeId,
            &resultStatus,
            &result);
        if (resultStatus.Code == rsError)
            throw std::runtime_error("CreateOperation failed: " + Utils::BSTRToString(resultStatus.Description));
        if (FAILED(hr))
            throw std::runtime_error("CreateOperation failed.");
        if (!result)
            throw std::runtime_error("CreateOperation returned nullptr.");
    }
    catch (const std::exception& ex) {
        std::cerr << "Error in CreateOperationObject(): " << ex.what() << std::endl;
        result = nullptr;
    }

    // return
    return result;
}

/// <summary>
/// Select geometry item by its identifier
/// </summary>
void SelectGeometryItemById(ICAMAPIGeometryModel* geometryModel,
    const std::vector<std::string>& geomIds) {
    TResultStatus resultStatus = {};

    // clear selection
    HRESULT hr = geometryModel->DeselectAll();
    if (FAILED(hr))
        throw std::runtime_error("DeselectAll failed.");

    // find and select items
    for (const auto& id : geomIds) {
        const std::wstring wid = Utils::ToWideUtf8(id);
        _bstr_t bstrId(wid.c_str());
        ICAMAPIGeometryTreeNode* node = nullptr;
        hr = geometryModel->FindByFullName(bstrId, &resultStatus, &node);
        if (resultStatus.Code == rsError)
            throw std::runtime_error("FindByFullName failed: " + Utils::BSTRToString(resultStatus.Description) + " for id: " + id);
        if (FAILED(hr))
            throw std::runtime_error("FindByFullName failed for id: " + id);
        if (!node)
            throw std::runtime_error("Cannot find geometry item by id: " + id);
        hr = node->put_Selected(true);
        if (FAILED(hr))
            throw std::runtime_error("Setting Selected property failed for id: " + id);
        node->Release();
    }
}

void CheckModelItemsAdded(ICamApiListModelItem* items) {
    if (!items)
        throw std::runtime_error("Items is nullptr.");
    long count;
    const HRESULT hr = items->get_Count(&count);
    if (const HRESULT hr = items->get_Count(&count); FAILED(hr))
        throw std::runtime_error("get_Count failed.");
    if (count == 0)
        throw std::runtime_error("No items were added");
}

HRESULT AddHolesToJobAssignment(ICamApiTechOperation* operation,
    ICAMAPIGeometryModel* geometryModel,
    const std::vector<std::string>& holesIds) {
    HRESULT result = S_OK;

    ICamApiModelFormer* modelFormer = nullptr;
    ICamApiModelFormerWithHoles* modelFormerWithHoles = nullptr;
    ICamApiListModelItem* items = nullptr;
    try {
        // select holes
        SelectGeometryItemById(geometryModel, holesIds);

        // add holes to the job assignment
        HRESULT hr = operation->get_ModelFormerJobAssignment(&modelFormer);
        if (FAILED(hr) || !modelFormer)
            throw std::runtime_error("get_ModelFormerJobAssignment failed.");

        hr = modelFormer->QueryInterface(IID_ICamApiModelFormerWithHoles, (void**)&modelFormerWithHoles);
        if (FAILED(hr) || !modelFormerWithHoles)
            throw std::runtime_error("QueryInterface for ICamApiModelFormerWithHoles failed.");

        hr = modelFormerWithHoles->AddHolesSelected(&items);
        if (FAILED(hr))
            throw std::runtime_error("AddHolesSelected failed.");
        CheckModelItemsAdded(items);
    }
    catch (const std::exception& ex) {
        std::cerr << "Error in AddHolesToJobAssignment(): " << ex.what() << std::endl;
        result = E_FAIL;
    }

    // clean
    if (items)
        items->Release();
    if (modelFormerWithHoles)
        modelFormerWithHoles->Release();
    if (modelFormer)
        modelFormer->Release();

    // return
    return result;
}

/// <summary>
/// Generate G code
/// </summary>
HRESULT GenerateGCode(ICamApiProject* project,
                      ICamApiTechnologist* technologist,
                      ICamApiPaths* pathsHelper) {
    HRESULT result = S_OK;
    TResultStatus resultStatus = {};

    ICamApiNCMaker* ncMaker = nullptr;
    ICamApiTechOperationIterator* operationIterator = nullptr;
    ICamApiMakeCncSettings* settings = nullptr;
    ICamApiMakeCncSppxSettings* settingsSppx = nullptr;
    BSTR postProcessorsFolder = nullptr;
    IListString* generateResult = nullptr;
    try {
        // get NCMaker
        HRESULT hr = project->get_NCMaker(&ncMaker);
        if (FAILED(hr) || !ncMaker)
            throw std::runtime_error("get_NCMaker failed.");

        // generate CLData file as first step
        hr = technologist->GetOperations(rmReordered, &resultStatus, &operationIterator);
        if (resultStatus.Code == rsError)
            throw std::runtime_error("GetOperations failed: " + Utils::BSTRToString(resultStatus.Description));
        if (FAILED(hr) || !operationIterator)
            throw std::runtime_error("GetOperations failed.");

        fs::path clDataFile = fs::temp_directory_path();
        std::string rnd = std::to_string(std::random_device{}());
        clDataFile = clDataFile / rnd;
        clDataFile.replace_extension(".inpcld");
        hr = project->SaveClData(_bstr_t(clDataFile.c_str()), operationIterator, &resultStatus);
        if (resultStatus.Code == rsError)
            throw std::runtime_error("SaveClData failed: " + Utils::BSTRToString(resultStatus.Description));
        if (FAILED(hr))
            throw std::runtime_error("SaveClData failed.");

        // make settings for CNC generating
        hr = ncMaker->CreateSettings(ncsSppx, &resultStatus, &settings);
        if (resultStatus.Code == rsError)
            throw std::runtime_error("CreateSettings failed: " + Utils::BSTRToString(resultStatus.Description));
        if (FAILED(hr) || !settings)
            throw std::runtime_error("CreateSettings failed.");

        hr = settings->QueryInterface(IID_ICamApiMakeCncSppxSettings, (void**)&settingsSppx);
        if (FAILED(hr) || !settingsSppx)
            throw std::runtime_error("QueryInterface for ICamApiMakeCncSppxSettings failed.");

        hr = settingsSppx->put_OutputFolder(_bstr_t(fs::temp_directory_path().c_str()));
        if (FAILED(hr))
            throw std::runtime_error("put_OutputFolder failed.");

        _bstr_t bstrNcFileName(L"result_nc_code.nc");
        hr = settingsSppx->put_NcFileName(bstrNcFileName);
        if (FAILED(hr))
            throw std::runtime_error("put_NcFileName failed.");

        auto resultNcCodeFile = fs::temp_directory_path() / "result_nc_code.nc";

        // get postprocessor from all users documents folder
        hr = pathsHelper->get_PostprocessorsFolder(&postProcessorsFolder);
        if (FAILED(hr))
            throw std::runtime_error("get_NcPostprocessorsFolderAllUsers failed.");
        auto postProcessorFilePath = fs::path(postProcessorsFolder) / "Mill" / "Fanuc (30i)_Mill.sppx";
        if (!fs::exists(postProcessorFilePath))
            throw std::runtime_error("Postprocessor not found: " + postProcessorFilePath.string());

        // generate CNC
        hr = ncMaker->Generate(_bstr_t(clDataFile.c_str()),
                               _bstr_t(postProcessorFilePath.c_str()),
                               settingsSppx,
                               &resultStatus,
                               &generateResult);
        if (resultStatus.Code == rsError)
            throw std::runtime_error("Generate failed: " + Utils::BSTRToString(resultStatus.Description));
        if (FAILED(hr))
            throw std::runtime_error("Generate failed.");

        // show result
        HINSTANCE hInst = ShellExecuteW(
            nullptr,
            L"open",
            L"notepad.exe",
            resultNcCodeFile.c_str(),
            nullptr,
            SW_SHOWNORMAL
        );

        if ((INT_PTR)hInst <= 32)
            throw std::runtime_error("Failed to start notepad.exe");

        std::cout << "G code generation completed." << std::endl;
    }
    catch (const std::exception& ex) {
        std::cerr << "Error in GenerateGCode(): " << ex.what() << std::endl;
        result = E_FAIL;
    }

    // clean
    if (generateResult)
        generateResult->Release();
    if (settingsSppx)
        settingsSppx->Release();
    if (settings)
        settings->Release();
    if (operationIterator)
        operationIterator->Release();
    if (ncMaker)
        ncMaker->Release();
    if (postProcessorsFolder)
        SysFreeString(postProcessorsFolder);

    // result
    return result;
}

BSTR CreateOperationFirst(ICamApiProject* project,
                          ICamApiTechnologist* technologist,
                          BSTR prevOperationId,
                          BSTR typeId) {
    BSTR result = nullptr;

    ICamApiTechOperation* newOperation = nullptr;
    try {
        // create operation
        newOperation = CreateOperationObject(technologist, prevOperationId, typeId);
        if (!newOperation)
            throw std::runtime_error("CreateOperationObject returned nullptr.");

        HRESULT hr = newOperation->get_Id(&result);
        if (FAILED(hr) || !result)
            throw std::runtime_error("get_Id failed.");

        // setup tool
        _bstr_t bstrToolId(L"12");
        SetupOperationTool(project, result, bstrToolId);

        std::cout << "Created 1st operation of type " << Utils::BSTRToString(typeId)
            << " with ID " << Utils::BSTRToString(result) << std::endl;
    }
    catch (const std::exception& ex) {
        std::cerr << "Error in CreateOperationFirst(): " << ex.what() << std::endl;
        result = nullptr;
    }

    // clean
    if (newOperation)
        newOperation->Release();

    // return
    return result;
}

BSTR CreateOperationSecond(ICamApiProject* project,
                           ICamApiTechnologist* technologist,
                           ICAMAPIGeometryModel* geometryModel,
                           BSTR prevOperationId,
                           BSTR typeId) {
    BSTR result = nullptr;

    ICamApiTechOperation* newOperation = nullptr;
    ICamApiModelFormer* modelFormer = nullptr;
    ICamApiModelFormerWithZones* modelFormerWithZones = nullptr;
    ICamApiListModelItem* items = nullptr;
    try {
        // create operation
        newOperation = CreateOperationObject(technologist, prevOperationId, typeId);
        if (!newOperation)
            throw std::runtime_error("CreateOperationObject returned nullptr.");

        HRESULT hr = newOperation->get_Id(&result);
        if (FAILED(hr) || !result)
            throw std::runtime_error("get_Id failed.");

        // setup tool
        _bstr_t bstrToolId(L"11");
        SetupOperationTool(project, result, bstrToolId);

        // setup job assignment for the operation
        std::vector<std::string> geomIds = { "Part\\Part1.igs\\Edge{Face11,Face7}" };
        SelectGeometryItemById(geometryModel, geomIds);

        hr = newOperation->get_ModelFormerJobAssignment(&modelFormer);
        if (FAILED(hr) || !modelFormer)
            throw std::runtime_error("get_ModelFormerJobAssignment failed.");

        hr = modelFormer->QueryInterface(IID_ICamApiModelFormerWithZones, (void**)&modelFormerWithZones);
        if (FAILED(hr) || !modelFormer)
            throw std::runtime_error("QueryInterface for ICamApiModelFormerWithZones failed.");

        hr = modelFormerWithZones->AddRestrictedZoneSelected(&items);
        if (FAILED(hr))
            throw std::runtime_error("AddRestrictedZoneSelected failed.");
        CheckModelItemsAdded(items);

        std::cout << "Created 2nd operation of type " << Utils::BSTRToString(typeId)
            << " with ID " << Utils::BSTRToString(result) << std::endl;
    }
    catch (const std::exception& ex) {
        std::cerr << "Error in CreateOperationSecond(): " << ex.what() << std::endl;
        result = nullptr;
    }

    // clean
    if (items)
        items->Release();
    if (modelFormerWithZones)
        modelFormerWithZones->Release();
    if (modelFormer)
        modelFormer->Release();
    if (newOperation)
        newOperation->Release();

    // return
    return result;
}

BSTR CreateOperationThird(ICamApiProject* project,
                          ICamApiTechnologist* technologist,
                          ICAMAPIGeometryModel* geometryModel,
                          BSTR prevOperationId,
                          BSTR typeId) {
    BSTR result = nullptr;

    ICamApiTechOperation* newOperation = nullptr;
    IST_XMLPropPointer* xmlProps = nullptr;
    try {
        // create operation
        newOperation = CreateOperationObject(technologist, prevOperationId, typeId);
        if (!newOperation)
            throw std::runtime_error("CreateOperationObject returned nullptr.");

        HRESULT hr = newOperation->get_Id(&result);
        if (FAILED(hr) || !result)
            throw std::runtime_error("get_Id failed.");

        // setup tool
        _bstr_t bstrToolId(L"11");
        SetupOperationTool(project, result, bstrToolId);

        // setup job assignment for the operation
        std::vector<std::string> geomIds = { "Part\\Part1.igs\\Face11" };
        AddHolesToJobAssignment(newOperation, geometryModel, geomIds);

        // setup properties
        hr = newOperation->get_XMLProp(&xmlProps);
        if (FAILED(hr) || !xmlProps)
            throw std::runtime_error("get_XMLProp failed.");

        hr = xmlProps->put_Str(_bstr_t(L"DrillingType"), _bstr_t(L"HolePocketing"));
        if (FAILED(hr))
            throw std::runtime_error("Setting DrillingType property failed.");

        std::cout << "Created 3rd operation of type " << Utils::BSTRToString(typeId)
            << " with ID " << Utils::BSTRToString(result) << std::endl;
    }
    catch (const std::exception& ex) {
        std::cerr << "Error in CreateOperationThird(): " << ex.what() << std::endl;
        result = nullptr;
    }

    // clean
    if (xmlProps)
        xmlProps->Release();
    if (newOperation)
        newOperation->Release();

    // return
    return result;
}

BSTR CreateOperationForth(ICamApiProject* project,
                          ICamApiTechnologist* technologist,
                          ICAMAPIGeometryModel* geometryModel,
                          BSTR prevOperationId,
                          BSTR typeId) {
    BSTR result = nullptr;

    ICamApiTechOperation* newOperation = nullptr;
    ICamApiModelFormer* modelFormer = nullptr;
    ICamApiModelFormerWithCurve2D* modelFormerWithCurve2D = nullptr;
    ICamApiModelFormerWithLevels* modelFormerWithLevels = nullptr;
    ICamApiListModelItem* items = nullptr;
    try {
        // create operation
        newOperation = CreateOperationObject(technologist, prevOperationId, typeId);
        if (!newOperation)
            throw std::runtime_error("CreateOperationObject returned nullptr.");

        HRESULT hr = newOperation->get_Id(&result);
        if (FAILED(hr) || !result)
            throw std::runtime_error("get_Id failed.");

        // setup tool
        _bstr_t bstrToolId(L"35");
        SetupOperationTool(project, result, bstrToolId);

        // setup job assignment for the operation
        hr = newOperation->get_ModelFormerJobAssignment(&modelFormer);
        if (FAILED(hr) || !modelFormer)
            throw std::runtime_error("get_ModelFormerJobAssignment failed.");

        // add the curve
        SelectGeometryItemById(geometryModel, { "Part\\Part1.igs\\Face6" });

        hr = modelFormer->QueryInterface(IID_ICamApiModelFormerWithCurve2D, (void**)&modelFormerWithCurve2D);
        if (FAILED(hr) || !modelFormerWithCurve2D)
            throw std::runtime_error("QueryInterface for ICamApiModelFormerWithCurve2D failed.");

        hr = modelFormerWithCurve2D->AddCurves2DSelected(&items);
        if (FAILED(hr))
            throw std::runtime_error("AddCurves2DSelected failed.");
        CheckModelItemsAdded(items);
        items->Release();
        items = nullptr;

        // add bottom level
        SelectGeometryItemById(geometryModel, { "Part\\Part1.igs\\Face12" });

        hr = modelFormer->QueryInterface(IID_ICamApiModelFormerWithLevels, (void**)&modelFormerWithLevels);
        if (FAILED(hr) || !modelFormerWithLevels)
            throw std::runtime_error("QueryInterface for ICamApiModelFormerWithLevels failed.");

        hr = modelFormerWithLevels->AddLevelSelected(amflBottomLevel, &items);
        if (FAILED(hr))
            throw std::runtime_error("AddLevelSelected failed.");
        CheckModelItemsAdded(items);

        std::cout << "Created 4th operation of type " << Utils::BSTRToString(typeId)
            << " with ID " << Utils::BSTRToString(result) << std::endl;
    }
    catch (const std::exception& ex) {
        std::cerr << "Error in CreateOperationForth(): " << ex.what() << std::endl;
        result = nullptr;
    }

    // clean
    if (items)
        items->Release();
    if (modelFormerWithLevels)
        modelFormerWithLevels->Release();
    if (modelFormerWithCurve2D)
        modelFormerWithCurve2D->Release();
    if (modelFormer)
        modelFormer->Release();
    if (newOperation)
        newOperation->Release();

    // return
    return result;
}

BSTR CreateOperationFifth(ICamApiProject* project,
                          ICamApiTechnologist* technologist,
                          ICAMAPIGeometryModel* geometryModel,
                          BSTR prevOperationId,
                          BSTR typeId) {
    BSTR result = nullptr;

    ICamApiTechOperation* newOperation = nullptr;
    try {
        // create operation
        newOperation = CreateOperationObject(technologist, prevOperationId, typeId);
        if (!newOperation)
            throw std::runtime_error("CreateOperationObject returned nullptr.");

        HRESULT hr = newOperation->get_Id(&result);
        if (FAILED(hr) || !result)
            throw std::runtime_error("get_Id failed.");

        // setup tool
        _bstr_t bstrToolId(L"75");
        SetupOperationTool(project, result, bstrToolId);

        // setup job assignment for the operation
        std::vector<std::string> geomIds = {
            "Part\\Part1.igs\\Face21",
            "Part\\Part1.igs\\Face4",
            "Part\\Part1.igs\\Face18",
            "Part\\Part1.igs\\Face19"
        };
        AddHolesToJobAssignment(newOperation, geometryModel, geomIds);

        std::cout << "Created 5th operation of type " << Utils::BSTRToString(typeId)
            << " with ID " << Utils::BSTRToString(result) << std::endl;
    }
    catch (const std::exception& ex) {
        std::cerr << "Error in CreateOperationFifth(): " << ex.what() << std::endl;
        result = nullptr;
    }

    // clean
    if (newOperation)
        newOperation->Release();

    // return
    return result;
}

BSTR CreateOperationSixth(ICamApiProject* project,
                          ICamApiTechnologist* technologist,
                          ICAMAPIGeometryModel* geometryModel,
                          BSTR prevOperationId,
                          BSTR typeId) {
    BSTR result = nullptr;

    ICamApiTechOperation* newOperation = nullptr;
    IST_XMLPropPointer* xmlProps = nullptr;
    try {
        // create operation
        newOperation = CreateOperationObject(technologist, prevOperationId, typeId);
        if (!newOperation)
            throw std::runtime_error("CreateOperationObject returned nullptr.");

        HRESULT hr = newOperation->get_Id(&result);
        if (FAILED(hr) || !result)
            throw std::runtime_error("get_Id failed.");

        // setup tool
        _bstr_t bstrToolId(L"63");
        SetupOperationTool(project, result, bstrToolId);

        // setup job assignment for the operation
        std::vector<std::string> geomIds = {
            "Part\\Part1.igs\\Face10",
            "Part\\Part1.igs\\Face2"
        };
        AddHolesToJobAssignment(newOperation, geometryModel, geomIds);

        // setup properties
        hr = newOperation->get_XMLProp(&xmlProps);
        if (FAILED(hr) || !xmlProps)
            throw std::runtime_error("get_XMLProp failed.");

        hr = xmlProps->put_Str(_bstr_t(L"DrillingType"), _bstr_t(L"ChipRemoving"));
        if (FAILED(hr))
            throw std::runtime_error("Setting DrillingType property failed.");

        std::cout << "Created 6th operation of type " << Utils::BSTRToString(typeId)
            << " with ID " << Utils::BSTRToString(result) << std::endl;
    }
    catch (const std::exception& ex) {
        std::cerr << "Error in CreateOperationSixth(): " << ex.what() << std::endl;
        result = nullptr;
    }

    // clean
    if (xmlProps)
        xmlProps->Release();
    if (newOperation)
        newOperation->Release();

    // return
    return result;
}

HRESULT TechnologyHelper::CreateTechnology(ICamApiApplication* application,
                                           ICamApiProject* project,
                                           ICamApiTechnologist* technologist,
                                           ICAMAPIGeometryModel* geom_model,
                                           ICamApiPaths* pathsHelper) {
    HRESULT result = S_OK;
    TResultStatus resultStatus = {};

    ICamApiTechOperation* operation = nullptr;
    BSTR operationRootId = nullptr;
    BSTR newOperationId = nullptr;
    try {
        // switch to model tab
        HRESULT hr = application->put_MainWorkMode(mwmMachining);
        if (FAILED(hr))
            throw std::runtime_error("put_MainWorkMode failed.");

        // setup workpiece
        hr = SetupWorkpiece(technologist);
        if (FAILED(hr))
            throw std::runtime_error("SetupStage failed.");

        // get root operation id
        hr = technologist->get_RootOperation(&operation);
        if (FAILED(hr))
            throw std::runtime_error("get_RootOperation failed.");
        if (!operation)
            throw std::runtime_error("Root operation is nullptr.");
        hr = operation->get_Id(&operationRootId);
        if (FAILED(hr))
            throw std::runtime_error("get_Id for root operation failed.");

        // type of operations
        _bstr_t bstrFaceMillingType(L"TSTFaceMillingOp");
        _bstr_t bstrRoughingWaterlineType(L"TSTRoughingWaterlineOp");
        _bstr_t bstrHoleMachiningType(L"HoleMachiningOp");
        _bstr_t bstr2DContouringType(L"TST2DContouringOp");

        // create operations
        newOperationId = CreateOperationFirst(project, technologist, operationRootId, bstrFaceMillingType);
        if (!newOperationId)
            throw std::runtime_error("CreateOperationFirst failed.");

        newOperationId = CreateOperationSecond(project, technologist, geom_model, newOperationId, bstrRoughingWaterlineType);
        if (!newOperationId)
            throw std::runtime_error("CreateOperationSecond failed.");

        newOperationId = CreateOperationThird(project, technologist, geom_model, newOperationId, bstrHoleMachiningType);
        if (!newOperationId)
            throw std::runtime_error("CreateOperationThird failed.");

        newOperationId = CreateOperationForth(project, technologist, geom_model, newOperationId, bstr2DContouringType);
        if (!newOperationId)
            throw std::runtime_error("CreateOperationThird failed.");

        newOperationId = CreateOperationFifth(project, technologist, geom_model, newOperationId, bstrHoleMachiningType);
        if (!newOperationId)
            throw std::runtime_error("CreateOperationThird failed.");

        newOperationId = CreateOperationSixth(project, technologist, geom_model, newOperationId, bstrHoleMachiningType);
        if (!newOperationId)
            throw std::runtime_error("CreateOperationThird failed.");

        // calculate entire technology
        hr = technologist->ResetAllOperationsToolpath();
        if (FAILED(hr))
            throw std::runtime_error("ResetAllOperationsToolpath failed.");

        hr = technologist->CalculateAllOperationsToolpath(true, &resultStatus);
        if (resultStatus.Code == rsError)
            throw std::runtime_error("CalculateAllOperationsToolpath failed: " + Utils::BSTRToString(resultStatus.Description));
        if (FAILED(hr))
            throw std::runtime_error("CalculateAllOperationsToolpath failed.");

        // generate CNC
        hr = GenerateGCode(project, technologist, pathsHelper);
        if (FAILED(hr))
            throw std::runtime_error("GenerateGCode failed.");

        std::cout << "Technology creation completed." << std::endl;
    }
    catch (const std::exception& ex) {
        std::cerr << "Error in CreateTechnology(): " << ex.what() << std::endl;
        result = E_FAIL;
    }

    // clean
    if (newOperationId)
        SysFreeString(newOperationId);
    if (operationRootId)
        SysFreeString(operationRootId);
    if (operation)
        operation->Release();

    // return
    return result;
}