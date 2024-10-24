unit Extension;

interface

uses
  System.SysUtils
  ,System.IOUtils
  ,Windows
  ,Winapi.ShellAPI
  ,Winapi.TlHelp32
  ,IDL.CAMAPI.Extensions
  ,IDL.CAMAPI.Application
  ,IDL.CAMAPI.ResultStatus
  ,IDL.CAMAPI.Singletons
;

type
  TExtensionTest = class(TInterfacedObject
    ,IExtension
    ,IExtensionUtility)
  private
    FExtensionInfo: IExtensionInfo;
  public
    constructor Create();
    destructor Destroy(); override;
  public
    function Get_Info: IExtensionInfo; safecall;
    procedure Set_Info(const Value: IExtensionInfo); safecall;
  public
    /// <summary>
    /// Show parameters of project
    /// </summary>
    /// <param name="context">Information about current running instance</param>
    /// <param name="resultStatus">Structure to return error</param>
    procedure Run(const Context: IExtensionUtilityContext; out ResultStatus: TResultStatus); safecall;
  end;

implementation

{ TExtensionTest }

constructor TExtensionTest.Create;
begin
  inherited Create();
  FExtensionInfo := nil;
end;

destructor TExtensionTest.Destroy;
begin
  //
  inherited;
end;

function TExtensionTest.Get_Info: IExtensionInfo;
begin
  Exit(FExtensionInfo);
end;

procedure TExtensionTest.Set_Info(const Value: IExtensionInfo);
begin
  FExtensionInfo := Value;
end;

procedure TExtensionTest.Run(const Context: IExtensionUtilityContext; out ResultStatus: TResultStatus);
begin
  ResultStatus := default(TResultStatus);

  try
    var ret := default(TResultStatus);

    // get global context
    if (FExtensionInfo = nil) then
      raise Exception.Create('Info is null');
    var extension := FExtensionInfo.InstanceInfo.ExtensionManager.GetSingletonExtension('Extension.Global.Singletons.Paths', ret);
    if (ret.Code = TResultStatusCode.rsError) then
      raise Exception.Create('Error getting global context: ' + ret.Description);
    var paths := extension as ICamApiPaths;

    // get context
    var currentFolder := paths.Get_MainProgramFolder;
    if currentFolder = '' then
      raise Exception.Create('Cannot get MainProgramFolder');

    // export
    var application := Context.CamApplication;
    var exportedFile := TPath.Combine(currentFolder, 'exported.stcp');
    application.ExportCurrentProject(exportedFile, true, ret);
    if (ret.Code = TResultStatusCode.rsError) then
      raise Exception.Create(ret.Description);
  except
    on e: exception do begin
      ResultStatus.Code := TResultStatusCode.rsError;
      ResultStatus.Description := e.Message;
    end;
  end;
end;

end.
