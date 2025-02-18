#ifndef EXTENSION_MANAGER_HELPER_H
#define EXTENSION_MANAGER_HELPER_H

#include "pch.h"
#include <windows.h>
#include <stdexcept>

#import <STTypes.tlb> no_namespace, named_guids
#import <STXmlPropTypes.tlb> no_namespace, named_guids
#import <STCustomPropTypes.tlb> no_namespace, named_guids
#import <CAMAPI.MeshTypes.tlb> no_namespace, named_guids
#import <CAMAPI.CurveTypes.tlb> no_namespace, named_guids
#import <CAMAPI.SurfaceTypes.tlb> no_namespace, named_guids
#import <CAMAPI.ModelFormerTypes.tlb> no_namespace, named_guids
#import <CAMAPI.Logger.tlb> no_namespace, named_guids
#import <CAMAPI.ResultStatus.tlb> no_namespace, named_guids
#import "CAMAPI.Generic.List.tlb" no_namespace, named_guids
#import "STGeomApiTypes.tlb" no_namespace, named_guids
#import "CAMAPI.Singletons.tlb" no_namespace, named_guids
#import "CAMAPI.Extensions.tlb" no_namespace, named_guids
#import "CAMAPI.NCMaker.tlb" no_namespace, named_guids
#import "CAMAPI.Machine.tlb" no_namespace, named_guids
#import "CAMAPI.TechOperation.tlb" no_namespace, named_guids
#import "CAMAPI.Technologist.tlb" no_namespace, named_guids
#import "CAMAPI.Snapshot.tlb" no_namespace, named_guids
#import "CAMAPI.GeomModel.tlb" no_namespace, named_guids
#import "CAMAPI.GeomModel.tlb" no_namespace, named_guids
#import "CAMAPI.GeomImporter.tlb" no_namespace, named_guids
#import "CAMAPI.ToolsList.tlb" no_namespace, named_guids
#import "CAMAPI.Project.tlb" no_namespace, named_guids
#import "CAMAPI.TechnologyForm.tlb" no_namespace, named_guids
#import "CAMAPI.ApplicationMainForm.tlb" no_namespace, named_guids
#import "CAMAPI.Extension.PLM.tlb" no_namespace, named_guids
#import "CAMAPI.Application.tlb" no_namespace, named_guids

class ExtensionManagerHelper {
private:
    static IExtensionManager* FExtensionManager;

public:
    static void Initialize();
    static void Finalize();
    static IExtensionManager* GetInstance();
};

extern struct StaticInitializer staticInitializer;

#endif // EXTENSION_MANAGER_HELPER_H
