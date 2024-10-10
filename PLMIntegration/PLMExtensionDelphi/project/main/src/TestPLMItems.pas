unit TestPLMItems;

interface

uses
  System.SysUtils, IDL.CAMAPI.Extension.PLM;

type
  TDataItem = record
    Id: string;
    Name: string;
    ItemType: TPLMItemType;
    TimeStamp: WideString;
    FilePath: WideString;
  end;
  TDataItems = array of TDataItem;

  TTestPLMTreeItem = class(TInterfacedObject, IPLMTreeItem)
  private
    FId: WideString;
    FName: WideString;
    FType: TPLMItemType;
    FComment: WideString;
    FChildsLoaded: WordBool;
    FChilds: IPLMTree;
    FView: Boolean;
    function IsView: Boolean;
  public
    destructor Destroy; override;

    function Put(AId, AName, AComment: WideString; AType: TPLMItemType): Boolean; overload;

    function Get_Id: WideString; safecall;
    function Get_Name: WideString; safecall;
    function Get_Type_: TPLMItemType; safecall;
    function Get_Comment: WideString; safecall;
    function Get_ChildsLoaded: WordBool; safecall;
    function Get_Childs: IPLMTree; safecall;

    property Id: WideString read Get_Id;
    property Name: WideString read Get_Name;
    property Type_: TPLMItemType read Get_Type_;
    property Comment: WideString read Get_Comment;
    property ChildsLoaded: WordBool read Get_ChildsLoaded;
    property Childs: IPLMTree read Get_Childs;
  end;

    TTestPLMTree = class(TInterfacedObject, IPLMTree)
  private
    FItems: array of IPLMTreeItem;
    procedure TestInit;
    procedure CreateItem(AId, AName, AComment: WideString; AType: TPLMItemType);
    procedure Add(AItem: TTestPLMTreeItem); overload;
  public
    constructor Create(AType: TPLMItemType = itNone);
    destructor Destroy; override;

    function Get_Count: Integer; safecall;
    function Get_Items(Index: Integer): IPLMTreeItem; safecall;
    property Count: Integer read Get_Count;
    property Items[Index: Integer]: IPLMTreeItem read Get_Items; default;
  end;

  TTestPLMDataItems = class(TInterfacedObject, IPLMDataItems)
  private
    FItems: array of IPLMDataItem;
  public
    destructor Destroy; override;
    constructor Create; overload;
    constructor Create(AItems : TDataItems); overload;

    procedure Add(AItem: IPLMDataItem);
    function Get_Count: Integer; safecall;
    function Get_Items(Index: Integer): IPLMDataItem; safecall;
    property Count: Integer read Get_Count;
    property Items[Index: Integer]: IPLMDataItem read Get_Items; default;
  end;

  TTestPLMFiles = class(TInterfacedObject, IPLMFiles)
  private
    FItems: array of WideString;
  public
    destructor Destroy; override;

    function Add(AFilePath: WideString): Boolean; overload;

    function Get_Count: Integer; safecall;
    function Get_Items(Index: Integer): WideString; safecall;
    property Count: Integer read Get_Count;
    property Items[Index: Integer]: WideString read Get_Items; default;
  end;

  TTestPLMDataItem = class(TInterfacedObject, IPLMDataItem)
  private
    FId: WideString;
    FName: WideString;
    FType: TPLMItemType;
    FTimeStamp: WideString;
    FFiles: IPLMFiles;
  public
    destructor Destroy; override;

    function Put(AId, AName, ATimeStamp: WideString; AType: TPLMItemType;
     AFiles: TTestPLMFiles): Boolean; overload;

    function Get_Id: WideString; safecall;
    function Get_Name: WideString; safecall;
    function Get_Type_: TPLMItemType; safecall;
    function Get_TimeStamp: WideString; safecall;
    function Get_Files: IPLMFiles; safecall;

    property Id: WideString read Get_Id;
    property Name: WideString read Get_Name;
    property Type_: TPLMItemType read Get_Type_;
    property TimeStamp: WideString read Get_TimeStamp;
    property Files: IPLMFiles read Get_Files;
  end;

implementation

{ TTestPLMTree }

constructor TTestPLMTree.Create(AType: TPLMItemType = itNone);
begin
  inherited Create;
  case AType of
    itNone: TestInit;
    itModel: CreateItem('NewModelId', 'NewModelName', '', itModel);
    itWorkpiece: CreateItem('NewWorkpieceId', 'NewWorkpieceName', '', itWorkpiece);
    itTool: CreateItem('NewToolId', 'NewToolName', '', itTool);
    itMachine: CreateItem('NewMachineId', 'NewMachineName', '', itMachine);
    itProject: CreateItem('NewProjectId', 'NewProjectName', '', itProject);
    itPostprocessor: CreateItem('NewPostprocessorId', 'NewPostprocessorName', '', itPostprocessor);
  end;

end;

destructor TTestPLMTree.Destroy;
var
  i: Integer;
begin
  for i := Low(FItems) to High(FItems) do
    FItems[i] := nil;
  SetLength(FItems, 0);
  inherited;
end;

function TTestPLMTree.Get_Count: Integer;
begin
  Result := Length(FItems);
end;

function TTestPLMTree.Get_Items(Index: Integer): IPLMTreeItem;
begin
  Result := FItems[Index];
end;

procedure TTestPLMTree.TestInit;
begin
  CreateItem('NoneId', 'NoneName', 'Test None PLMTree Item', itNone);
  CreateItem('ModelId', 'ModelName', 'Test Model PLMTree Item', itModel);
  CreateItem('WorkpieceId', 'WorkpieceName', 'Test Workpiece PLMTree Item', itWorkpiece);
  CreateItem('ToolId', 'ToolName', 'Test Tool PLMTree Item', itTool);
  CreateItem('MachineId', 'MachineName', 'Test Machine PLMTree Item', itMachine);
  CreateItem('ProjectId', 'ProjectName', 'Test Project PLMTree Item', itProject);
  CreateItem('PostprocessorId', 'PostprocessorName', 'Test Postprocessor PLMTree Item', itPostprocessor);
end;

procedure TTestPLMTree.CreateItem(AId, AName, AComment: WideString;
  AType: TPLMItemType);
var
  Item: TTestPLMTreeItem;
begin
  Item := TTestPLMTreeItem.Create;
  if Item.Put(AId, AName, AComment, AType) then
    Add(Item)
  else
    Item.Free;
end;

procedure TTestPLMTree.Add(AItem: TTestPLMTreeItem);
begin
  SetLength(FItems, Succ(Length(FItems)));
  FItems[High(FItems)] := AItem;
end;

{ TTestPLMTreeItem }

destructor TTestPLMTreeItem.Destroy;
begin
  FChilds := nil;
  inherited;
end;

function TTestPLMTreeItem.Get_Childs: IPLMTree;
begin
  Result := FChilds;
end;

function TTestPLMTreeItem.Get_ChildsLoaded: WordBool;
begin
  Result := FChildsLoaded;
end;

function TTestPLMTreeItem.Get_Comment: WideString;
begin
  Result := FComment;
end;

function TTestPLMTreeItem.Get_Id: WideString;
begin
  Result := FId;
end;

function TTestPLMTreeItem.Get_Name: WideString;
begin
  Result := FName;
end;

function TTestPLMTreeItem.Get_Type_: TPLMItemType;
begin
  Result := FType;
end;

function TTestPLMTreeItem.IsView: Boolean;
begin
  Result := FView;
end;

function TTestPLMTreeItem.Put(AId, AName, AComment: WideString; AType: TPLMItemType): Boolean;
begin
  Result := True;
  try
    FId := AId;
    FName := AName;
    FComment := AComment;
    FType := AType;
    FChilds := nil;
    FChildsLoaded := True;
  except
    Result := False;
  end;
end;

{ TTestPLMDataItems }

procedure TTestPLMDataItems.Add(AItem: IPLMDataItem);
begin
  SetLength(FItems, Succ(Length(FItems)));
  FItems[High(FItems)] := AItem;
end;

constructor TTestPLMDataItems.Create(AItems: TDataItems);
var
  i: Integer;
  DataItem: TTestPLMDataItem;
  Files: TTestPLMFiles;
begin
  inherited Create;
  if Assigned(AItems) then begin
    SetLength(FItems, Length(AItems));
    for i := Low(FItems) to High(FItems) do begin
      Files := TTestPLMFiles.Create;
      Files.Add(AItems[i].FilePath);
      DataItem := TTestPLMDataItem.Create;
      DataItem.Put(AItems[i].Id, AItems[i].Name, AItems[i].TimeStamp,
      AItems[i].ItemType, Files);
      FItems[i] := DataItem;
    end;
  end;
end;

constructor TTestPLMDataItems.Create;
begin
  inherited;
end;

destructor TTestPLMDataItems.Destroy;
begin
  FItems := nil;
  inherited;
end;

function TTestPLMDataItems.Get_Count: Integer;
begin
  Result := Length(FItems);
end;

function TTestPLMDataItems.Get_Items(Index: Integer): IPLMDataItem;
begin
  Result := FItems[Index];
end;

{ TTestPLMDataItem }

destructor TTestPLMDataItem.Destroy;
begin
  FFiles := nil;
  inherited;
end;

function TTestPLMDataItem.Get_Files: IPLMFiles;
begin
  Result := FFiles;
end;

function TTestPLMDataItem.Get_Id: WideString;
begin
  Result := FId;
end;

function TTestPLMDataItem.Get_Name: WideString;
begin
  Result := FName;
end;

function TTestPLMDataItem.Get_TimeStamp: WideString;
begin
  Result := FTimeStamp;
end;

function TTestPLMDataItem.Get_Type_: TPLMItemType;
begin
  Result := FType;
end;

function TTestPLMDataItem.Put(AId, AName, ATimeStamp: WideString;
  AType: TPLMItemType; AFiles: TTestPLMFiles): Boolean;
begin
  Result := True;
  try
    FId := AId;
    FName := AName;
    FTimeStamp := ATimeStamp;
    FType := AType;
    FFiles := AFiles;
  except
    Result := False;
  end;
end;

{ TTestPLMFiles }

function TTestPLMFiles.Add(AFilePath: WideString): Boolean;
begin
  SetLength(FItems, Succ(Length(FItems)));
  FItems[High(FItems)] := AFilePath;
end;

destructor TTestPLMFiles.Destroy;
begin
  FItems := nil;
  inherited;
end;

function TTestPLMFiles.Get_Count: Integer;
begin
  Result := Length(FItems);
end;

function TTestPLMFiles.Get_Items(Index: Integer): WideString;
begin
  Result := FItems[Index]
end;

end.
