unit TestPLMInterface;

interface

uses
  System.SysUtils,
  IOUtils,
  IDL.CAMAPI.Extension.PLM,
  IDL.CAMAPI.Extensions,
  TestPLMParameters,
  TestPLMItems;

type
  TPLMResult = class(TInterfacedObject, IPLMResult)
  private
    FCode: Integer;
    FErrorMessage: WideString;
    FWarningMessage: WideString;

    procedure Set_Code(ACode: Integer);
    procedure Set_WarningMessage(AWarningMessage: WideString);
    procedure Set_ErrorMessage(AErrorMessage: WideString);

    procedure Set_Successful(AWarningMessage: WideString);
    procedure Parse_Exception(E: Exception);
    function Check_License: Boolean;
  public
    function Get_Code: Integer; safecall;
    function Get_ErrorMessage: WideString; safecall;
    function Get_WarningMessage: WideString; safecall;
  end;

  TTestPLMInterface = class(TInterfacedObject, IExtensionPLM, IPLMInterface, IExtension)
  private
    FInfo: IExtensionInfo;
  public
    constructor Create;
    procedure SetLanguage(LanguageID: LongWord; LngCodePage: Byte); safecall;

    function GetParameters: IPLMParameters; safecall;
    function Connect(const Values: IPLMParameterValues): IPLMResult; safecall;
    function Disconnect: IPLMResult; safecall;
    function Install: IPLMResult; safecall;
    function Uninstall: IPLMResult; safecall;

    function GetItem(ItemType: TPLMItemType; const ItemId: WideString; out Items: IPLMTree): IPLMResult; safecall;
    function GetLinkedItem(ItemType: TPLMItemType; LinkedItemType: TPLMItemType;
                           const ItemId: WideString; out Items: IPLMTree): IPLMResult; safecall;
    function GetChilds(ItemType: TPLMItemType; const ParentItemId: WideString; out Items: IPLMTree): IPLMResult; safecall;
    function FindItems(ItemType: TPLMItemType; const ItemName: WideString; out Items: IPLMTree): IPLMResult; safecall;

    function DownloadItems(const Items: IPLMItems; const FilePath: WideString;
                           out DwnItems: IPLMDataItems): IPLMResult; safecall;
    function DownloadProject(const ItemId: WideString; const FilePath: WideString;
                             out DwnItems: IPLMDataItems;
                             out PrjStructItems: IPLMProjectStructItems): IPLMResult; safecall;

    function UploadProject(const Project: IPLMCAMProject; SaveAs: WordBool; Replace: WordBool): IPLMResult; safecall;
    function UploadItem(ItemType: TPLMItemType; const ItemId: WideString; const Files: IPLMFiles;
                        Replace: WordBool; out UplItems: IPLMDataItems): IPLMResult; safecall;

    function Get_Info: IExtensionInfo; safecall;
    procedure Set_Info(const Value: IExtensionInfo); safecall;
  end;

const
  SHost = 'Host';
  SPort = 'Port';
  SLogin = 'Login';
  SPassword = 'Password';

  PLMExtensionTempDir = '..\PLMExtensionData';

  SPPDataItemName = 'Fanuc (30i)_Mill';
  SPPFilePath = PLMExtensionTempDir + '\Fanuc (30i)_Mill.sppx';
  ModelDataItemName = '49-1 Test';
  ModelFilePath = PLMExtensionTempDir + '\49-1.igs';
  MachineDataItemName = 'DMU60 Test';
  MachineFilePath = PLMExtensionTempDir + '\DMU60.zip';
  ToolDataItemName = 'Drill Test';
  ToolFilePath = PLMExtensionTempDir + '\Drill.sc12tool';

implementation

var
  ConnectionParameters: TConnectionParameters;
  SettingsParameters: TSettingsParameters;
  LoginParameters: TLoginParameters;
  LocaleListOfValues: TLoginParamValues;

procedure AddConParam(const AId, AName, ADefaultValue: string; const AOrder: Integer);
begin
  SetLength(ConnectionParameters, Succ(Length(ConnectionParameters)));
  with ConnectionParameters[High(ConnectionParameters)] do begin
    Id := AId;
    Name := AName;
    DefaultValue := ADefaultValue;
    Order := AOrder;
  end;
end;

procedure AddLogParam(const AId, AName: string; const AMandatory, APassword: Boolean;
  const ADefaultValue: string; const AOrder: Integer);
begin
  SetLength(LoginParameters, Succ(Length(LoginParameters)));
  with LoginParameters[High(LoginParameters)] do begin
    Id := AId;
    Name := AName;
    Mandatory := AMandatory;
    Password := APassword;
    DefaultValue := ADefaultValue;
    Order := AOrder;
  end;
end;

procedure InitParameters;
begin
  SetLength(ConnectionParameters, 0);
  AddConParam(SHost,    SHost,    'myhost', 0);
  AddConParam(SPort,    SPort,    '7001',         1);
  SetLength(LoginParameters, 0);
  AddLogParam(SLogin,    SLogin,    True,  False, '', 0);
  AddLogParam(SPassword, SPassword, True,  True,  '', 1);
end;

function AddDwnDataItem(AId, AName, AFilePath: WideString; AType : TPLMItemType): TDataItems;
var
  DwnDataItems: TDataItems;
begin
  SetLength(DwnDataItems, Succ(Length(DwnDataItems)));
  with DwnDataItems[High(DwnDataItems)] do begin
    Id := AId;
    Name := AName;
    ItemType := AType;
    TimeStamp := '';
    FilePath := AFilePath;
  end;
  Result := DwnDataItems;
end;

{ TTestPLMInterface }

constructor TTestPLMInterface.Create;
begin
  inherited;
  InitParameters;
end;

function TTestPLMInterface.Install: IPLMResult;
begin
  Result := TPLMResult.Create;
end;

function TTestPLMInterface.Uninstall: IPLMResult;
var
  Res : TPLMResult;
begin
  Res := TPLMResult.Create;
  Res.Set_Successful('');
  Result := Res;
end;

procedure TTestPLMInterface.SetLanguage(LanguageID: LongWord; LngCodePage:Byte);
begin
end;

procedure TTestPLMInterface.Set_Info(const Value: IExtensionInfo);
begin
  FInfo := Value;
end;

function TTestPLMInterface.GetParameters: IPLMParameters;
begin
  Result := TTestPLMParameters.Create(
    ConnectionParameters,
    LoginParameters,
    LocaleListOfValues,
    SettingsParameters
    );
end;

function TTestPLMInterface.Get_Info: IExtensionInfo;
begin
  Exit(FInfo);
end;

function TTestPLMInterface.Connect(
  const Values: IPLMParameterValues): IPLMResult;
var
  Res : TPLMResult;
begin
  Res := TPLMResult.Create;
  Res.Set_Successful('');
  Result := Res;
end;

function TTestPLMInterface.Disconnect: IPLMResult;
var
  Res : TPLMResult;
begin
  Res := TPLMResult.Create;
  Res.Set_Successful('');
  Result := Res;
end;

function TTestPLMInterface.DownloadItems(const Items: IPLMItems;
  const FilePath: WideString; out DwnItems: IPLMDataItems): IPLMResult;
var
  i : Integer;
  Res : TPLMResult;
  DwnDataItems : TDataItems;
begin
  for i := 0 to Pred(Items.Count) do
    case Items[i].Type_ of
      itModel: DwnDataItems := AddDwnDataItem(ModelDataItemName, ModelDataItemName, ModelFilePath, itModel);
      itWorkpiece: DwnDataItems := AddDwnDataItem(ModelDataItemName, ModelDataItemName, ModelFilePath, itWorkpiece);
      itTool: DwnDataItems := AddDwnDataItem(ToolDataItemName, ToolDataItemName, ToolFilePath, itTool);
      itMachine: DwnDataItems := AddDwnDataItem(MachineDataItemName, MachineDataItemName, MachineFilePath, itMachine);
      itProject: ;
      itPostprocessor: DwnDataItems := AddDwnDataItem(SPPDataItemName, SPPDataItemName, SPPFilePath, itPostprocessor);
    end;
  DwnItems := TTestPLMDataItems.Create(DwnDataItems);
  Res := TPLMResult.Create;
  Res.Set_Successful('');
  Result := Res;
end;

function TTestPLMInterface.DownloadProject(const ItemId, FilePath: WideString;
  out DwnItems: IPLMDataItems;
  out PrjStructItems: IPLMProjectStructItems): IPLMResult;
var
  Res : TPLMResult;
begin
  DwnItems := TTestPLMDataItems.Create();
  Res := TPLMResult.Create;
  Res.Set_Successful('');
  Result := Res;
end;

function TTestPLMInterface.FindItems(ItemType: TPLMItemType;
  const ItemName: WideString; out Items: IPLMTree): IPLMResult;
var
  Res : TPLMResult;
begin
  Items := TTestPLMTree.Create;
  Res := TPLMResult.Create;
  Res.Set_Successful('');
  Result := Res;
end;

function TTestPLMInterface.GetChilds(ItemType: TPLMItemType;
  const ParentItemId: WideString; out Items: IPLMTree): IPLMResult;
var
  Res : TPLMResult;
begin
  Items := TTestPLMTree.Create;
  Res := TPLMResult.Create;
  Res.Set_Successful('');
  Result := Res;
end;

function TTestPLMInterface.GetItem(ItemType: TPLMItemType;
  const ItemId: WideString; out Items: IPLMTree): IPLMResult;
var
  Res : TPLMResult;
begin
  Items := TTestPLMTree.Create;
  Res := TPLMResult.Create;
  Res.Set_Successful('');
  Result := Res;
end;

function TTestPLMInterface.GetLinkedItem(ItemType, LinkedItemType: TPLMItemType;
  const ItemId: WideString; out Items: IPLMTree): IPLMResult;
var
  Res : TPLMResult;
begin
  if LinkedItemType = itPostprocessor then
    Items := TTestPLMTree.Create(itPostprocessor)
  else
    Items := TTestPLMTree.Create;
  Res := TPLMResult.Create;
  Res.Set_Successful('');
  Result := Res;
end;

function TTestPLMInterface.UploadItem(ItemType: TPLMItemType;
  const ItemId: WideString; const Files: IPLMFiles; Replace: WordBool;
  out UplItems: IPLMDataItems): IPLMResult;
var
  Res : TPLMResult;
  i : Integer;
begin
  for i := 0 to Pred(Files.Count) do
    TFile.Copy(Files[i], TPath.Combine(PLMExtensionTempDir, ExtractFileName(Files[i])), true);
  UplItems := TTestPLMDataItems.Create();
  Res := TPLMResult.Create;
  Res.Set_Successful('');
  Result := Res;
end;

function TTestPLMInterface.UploadProject(const Project: IPLMCAMProject; SaveAs,
  Replace: WordBool): IPLMResult;
var
  Res : TPLMResult;
begin
  Res := TPLMResult.Create;
  Res.Set_Successful('');
  Result := Res;
end;

{ TPLMResult }

function TPLMResult.Check_License: Boolean;
begin
  Result := True;
end;

function TPLMResult.Get_Code: Integer;
begin
  Result := FCode;
end;

function TPLMResult.Get_ErrorMessage: WideString;
begin
  Result := FErrorMessage;
end;

function TPLMResult.Get_WarningMessage: WideString;
begin
  Result := FWarningMessage;
end;

procedure TPLMResult.Parse_Exception(E: Exception);
begin

end;

procedure TPLMResult.Set_Code(ACode: Integer);
begin
  FCode := ACode;
end;

procedure TPLMResult.Set_ErrorMessage(AErrorMessage: WideString);
begin
  FErrorMessage := AErrorMessage;
end;

procedure TPLMResult.Set_WarningMessage(AWarningMessage: WideString);
begin
  FWarningMessage := AWarningMessage;
end;

procedure TPLMResult.Set_Successful(AWarningMessage: WideString);
begin
  FCode := 0;
  FErrorMessage := '';
  FWarningMessage := AWarningMessage;
end;

end.