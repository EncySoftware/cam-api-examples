#pragma once
#include "oaidl.h"
#include "ocidl.h"
#include <Unknwn.h>

// The generated SDK headers reference each other in cycles, so no include order alone resolves them.
interface ICadApiBody;
interface ICadApiExtrudeFeature;
interface ICadApiFeature;
interface ICadApiModel;
interface ICAMAPIGeometryEntity;
interface ICAMAPIProjectEtalonReceiver;
interface ICamApiFeature;
interface ICamApiFeatureFinder;
interface ICamApiMachineConfiguration;
interface ICamApiMacroBuilderLanguageSettings;
interface ICamApiMacroBuilderSettings;
interface ICamApiMacroCommandData;
interface ICamApiMacroManager;
interface ICamApiModelFormer;
interface ICamApiTechOperation;
interface ICamApiUserTechOperationList;
interface ICamApiViewCube;
interface ICamApiViewPort;
interface ICamApiWorkpieceSetup;

#include "CAMAPI.ResultStatus.h"
#include <CAMAPI.EventHandler.h>
#include <CAMAPI.Generic.List.h>
#include <STTypes.h>
#include <STXMLPropTypes.h>
#include <CAMAPI.Machine.h>
#include <CAMAPI.CurveTypes.h>
#include <CAMAPI.MeshTypes.h>
#include <CAMAPI.SurfaceTypes.h>
#include <CAMAPI.ModelFormerTypes.h>
#include <CAMAPI.CustomAttributes.h>
#include <STCustomPropTypes.h>
#include <CAMAPI.Tools.h>
#include <CAMAPI.Workpiece.h>
#include <CAMAPI.MachineConfiguration.h>
#include <CAMAPI.MCDFormerTypes.h>
#include <CAMAPI.TechOperation.h>
#include <CAMAPI.PartStage.h>
#include <CAMAPI.Technologist.h>
#include <CAMAPI.NCMaker.h>
#include <CAMAPI.EtalonProject.h>
#include <CAMAPI.GeomModel.h>
#include <CAMAPI.GeomLibrary.h>
#include <CAMAPI.GeomImporter.h>
#include <CAMAPI.ToolsList.h>
#include <CAMAPI.Snapshot.h>
#include <CAMAPI.CoordinateSystem.h>
#include <CAMAPI.Simulator.h>
#include <CAMAPI.Project.h>
#include <CAMAPI.TechnologyForm.h>
#include <CAMAPI.ApplicationMainForm.h>
#include <CAMAPI.Logger.h>
#include <STGeomApiTypes.h>
#include <CAMAPI.Singletons.h>
#include <CAMAPI.Extensions.h>
#include <CAMAPI.Extension.PLM.h>
#include <CAMAPI.MachinesLibrary.h>
#include <CAMAPI.Utilities.h>
#include <CAMAPI.Application.h>

inline const IID IID_ICamApiPaths = { 0x89101f8a, 0x17dd, 0x418a, { 0xba, 0xf9, 0xef, 0xbd, 0x77, 0x98, 0x2a, 0x92 } };
inline const IID IID_ICamApiModelFormerWithLevels = { 0x66bf6482, 0xd829, 0x4733, { 0xbb, 0x35, 0x48, 0xa7, 0x7c, 0xc1, 0x54, 0x15 } };
inline const IID IID_ICamApiModelFormerWithZones = { 0x53be51c1, 0xf86a, 0x43ba, { 0xac, 0xd2, 0x54, 0x0c, 0x08, 0x46, 0x07, 0xb8 } };
inline const IID IID_ICamApiModelFormerWithHoles ={ 0xebd95d9f, 0x12d6, 0x41c7, { 0x9e, 0xdc, 0xe3, 0x49, 0x58, 0x85, 0x31, 0xaf } };
inline const IID IID_ICamApiModelFormerWithCurve2D = { 0x36d53e06, 0xbb05, 0x4d62, { 0xa4, 0x7f, 0xed, 0xf5, 0xb5, 0x09, 0x44, 0xed } };
inline const IID IID_ICamApiMakeCncSppxSettings = { 0x5d1ea41a, 0x0b6e, 0x4be3, { 0xba, 0x5d, 0x13, 0x52, 0xb8, 0xd0, 0xdf, 0x52 } };
