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
;

type
  TExtensionUtility = class(TInterfacedObject
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

{ TExtensionUtility }

constructor TExtensionUtility.Create;
begin
  inherited Create();
  FExtensionInfo := nil;
end;

destructor TExtensionUtility.Destroy;
begin
  //
  inherited;
end;

function TExtensionUtility.Get_Info: IExtensionInfo;
begin
  Exit(FExtensionInfo);
end;

procedure TExtensionUtility.Set_Info(const Value: IExtensionInfo);
begin
  FExtensionInfo := Value;
end;

procedure TExtensionUtility.Run(const Context: IExtensionUtilityContext; out ResultStatus: TResultStatus);
begin
  ResultStatus := default(TResultStatus);

  try
    // get project
    var project := context.CamApplication.GetActiveProject(resultStatus);
    if (resultStatus.Code = TResultStatusCode.rsError) then
        raise Exception.Create('Error getting project: ' + resultStatus.Description);

    // save params in some temp file to show it later
    var guid: TGUID;
    CreateGUID(guid);
    var tempFile := TPath.Combine(TPath.GetTempPath(), guid.ToString + '.txt');
    TFile.WriteAllText(tempFile, 'Project file path: ' + project.FilePath + sLineBreak);
    TFile.AppendAllText(tempFile, 'Project id: ' + project.Id);

    // show temp file
    ShellExecute(0, 'open', 'notepad.exe', PWideChar(tempFile), nil, SW_SHOWNORMAL);

    // free memory
    project := nil;
  except
    on e: exception do begin
      ResultStatus.Code := TResultStatusCode.rsError;
      ResultStatus.Description := e.Message;
    end;
  end;
end;

end.
