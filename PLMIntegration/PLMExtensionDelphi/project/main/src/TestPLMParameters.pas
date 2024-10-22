unit TestPLMParameters;

interface

uses
  System.SysUtils, IDL.CAMAPI.Extension.PLM;

type
  TConnectionParameter = record
    Id: string;
    Name: string;
    DefaultValue: string;
    Order: Integer;
  end;
  TConnectionParameters = array of TConnectionParameter;

  TLoginParamValue = record
    Value: string;
    DisplayName: string;
  end;
  TLoginParamValues = array of TLoginParamValue;

  TLoginParameter = record
    Id: string;
    Name: string;
    Mandatory: Boolean;
    Password: Boolean;
    DefaultValue: string;
    Order: Integer;
  end;
  TLoginParameters = array of TLoginParameter;

  TSettingsParameter = record
    Id: string;
    Name: string;
    DefaultValue: string;
    Order: Integer;
  end;
  TSettingsParameters = array of TSettingsParameter;

  TTestPLMParameters = class(TInterfacedObject, IPLMParameters)
  private
    FCParameters: IPLMConnectionParameters;
    FLParameters: IPLMLoginParameters;
    FSParameters: IPLMSettingsParameters;
    FProjectPreview: IPLMProjectPreview;
  public
    constructor Create(
      AConnection: TConnectionParameters;
      ALogin: TLoginParameters;
      ALoginValues: TLoginParamValues;
      ASettings: TSettingsParameters); reintroduce;
    destructor Destroy; override;
    function Get_Connection: IPLMConnectionParameters; safecall;
    function Get_Login: IPLMLoginParameters; safecall;
    function Get_Settings: IPLMSettingsParameters; safecall;
    function Get_ProjectPreview: IPLMProjectPreview; safecall;
  end;

  TTestPLMConnectionParameters = class(TInterfacedObject, IPLMConnectionParameters)
  private
    FParams: array of IPLMConnectionParameter;
    procedure Load(AConnection: TConnectionParameters);
  public
    destructor Destroy; override;
    function Get_Count: Integer; safecall;
    function Get_Items(Index: Integer): IPLMConnectionParameter; safecall;
  end;

  TTestPLMConnectionParameter = class(TInterfacedObject, IPLMConnectionParameter)
  private
    FId: WideString;
    FName: WideString;
    FDefaultValue: WideString;
    FOrder: Integer;
    procedure Load(AConnectionParameter: TConnectionParameter);
  public
    function Get_Id: WideString; safecall;
    function Get_Name: WideString; safecall;
    function Get_DefaultValue: WideString; safecall;
    function Get_Order: Integer; safecall;
  end;

  TTestPLMLoginParameters = class(TInterfacedObject, IPLMLoginParameters)
  private
    FParams: array of IPLMLoginParameter;
    procedure Load(ALogin: TLoginParameters; ALoginValues: TLoginParamValues);
  public
    destructor Destroy; override;
    function Get_Count: Integer; safecall;
    function Get_Items(Index: Integer): IPLMLoginParameter; safecall;
  end;

  TTestPLMLoginParameter = class(TInterfacedObject, IPLMLoginParameter)
  private
    FId: WideString;
    FName: WideString;
    FDefaultValue: WideString;
    FOrder: Integer;
    FMandatory: Boolean;
    FPassword: Boolean;
    FLOV: IPLMLoginParamListOfValues;
    procedure Load(ALoginParameter: TLoginParameter);
    procedure Set_LOV(ALoginValues: TLoginParamValues);
  public
    destructor Destroy; override;
    function Get_Id: WideString; safecall;
    function Get_Name: WideString; safecall;
    function Get_DefaultValue: WideString; safecall;
    function Get_Order: Integer; safecall;
    function Get_Mandatory: WordBool; safecall;
    function Get_Password: WordBool; safecall;
    function Get_LOV: IPLMLoginParamListOfValues; safecall;
  end;

  TTestPLMLoginParamListOfValues = class(TInterfacedObject, IPLMLoginParamListOfValues)
  private
    FLOVs: array of IPLMLoginParamValue;
    procedure Load(ALoginValues: TLoginParamValues);
  public
    destructor Destroy; override;
    function Get_Count: Integer; safecall;
    function Get_Items(Index: Integer): IPLMLoginParamValue; safecall;
  end;

  TTestPLMLoginParamValue = class(TInterfacedObject, IPLMLoginParamValue)
  private
    FValue: WideString;
    FDisplayName: WideString;
    procedure Set_Value(ALoginValue: TLoginParamValue);
  public
    function Get_Value: WideString; safecall;
    function Get_DisplayName: WideString; safecall;
  end;

  TTestPLMSettingsParameters = class(TInterfacedObject, IPLMSettingsParameters)
  private
    FParams: array of IPLMSettingsParameter;
    procedure Load(ASettings: TSettingsParameters);
  public
    destructor Destroy; override;
    function Get_Count: Integer; safecall;
    function Get_Items(Index: Integer): IPLMSettingsParameter; safecall;
  end;

  TTestPLMSettingsParameter = class(TInterfacedObject, IPLMSettingsParameter)
  private
    FId: WideString;
    FName: WideString;
    FDefaultValue: WideString;
    FOrder: Integer;
    procedure Load(ASettingsParameter: TSettingsParameter);
  public
    function Get_Id: WideString; safecall;
    function Get_Name: WideString; safecall;
    function Get_DefaultValue: WideString; safecall;
    function Get_Order: Integer; safecall;
  end;

  TTestPLMProjectPreview = class(TInterfacedObject, IPLMProjectPreview)
  public
    function Get_Width: Integer; safecall;
    function Get_Height: Integer; safecall;
  end;

implementation

{ TTestPLMParameters }

constructor TTestPLMParameters.Create(AConnection: TConnectionParameters;
  ALogin: TLoginParameters; ALoginValues: TLoginParamValues;
  ASettings: TSettingsParameters);
var
  CParams: TTestPLMConnectionParameters;
  LParams: TTestPLMLoginParameters;
  SParams: TTestPLMSettingsParameters;
begin
  inherited Create;

  CParams := TTestPLMConnectionParameters.Create;
  CParams.Load(AConnection);
  FCParameters := CParams;

  LParams := TTestPLMLoginParameters.Create;
  LParams.Load(ALogin, ALoginValues);
  FLParameters := LParams;

  SParams := TTestPLMSettingsParameters.Create;
  SParams.Load(ASettings);
  FSParameters := SParams;

  FProjectPreview := TTestPLMProjectPreview.Create;
end;

destructor TTestPLMParameters.Destroy;
begin
  FCParameters := nil;
  FLParameters := nil;
  FSParameters := nil;
  FProjectPreview := nil;
  inherited;
end;

function TTestPLMParameters.Get_Connection: IPLMConnectionParameters;
begin
  Result := FCParameters;
end;

function TTestPLMParameters.Get_Login: IPLMLoginParameters;
begin
  Result := FLParameters;
end;

function TTestPLMParameters.Get_ProjectPreview: IPLMProjectPreview;
begin
  Result := FProjectPreview;
end;

function TTestPLMParameters.Get_Settings: IPLMSettingsParameters;
begin
  Result := FSParameters;
end;

{ TTestPLMConnectionParameters }

destructor TTestPLMConnectionParameters.Destroy;
var
  i: Integer;
begin
  for i := Low(FParams) to High(FParams) do
    FParams[i] := nil;
  inherited;
end;

function TTestPLMConnectionParameters.Get_Count: Integer;
begin
  Result := Length(FParams);
end;

function TTestPLMConnectionParameters.Get_Items(
  Index: Integer): IPLMConnectionParameter;
begin
  Result := FParams[Index];
end;

procedure TTestPLMConnectionParameters.Load(AConnection: TConnectionParameters);
var
  i: Integer;
  Parameter: TTestPLMConnectionParameter;
begin
  SetLength(FParams, Length(AConnection));
  for i := Low(FParams) to High(FParams) do begin
    Parameter := TTestPLMConnectionParameter.Create;
    Parameter.Load(AConnection[i]);
    FParams[i] := Parameter;
  end;
end;

{ TTestPLMConnectionParameter }

function TTestPLMConnectionParameter.Get_DefaultValue: WideString;
begin
  Result := FDefaultValue;
end;

function TTestPLMConnectionParameter.Get_Id: WideString;
begin
  Result := FId;
end;

function TTestPLMConnectionParameter.Get_Name: WideString;
begin
  Result := FName;
end;

function TTestPLMConnectionParameter.Get_Order: Integer;
begin
  Result := FOrder;
end;

procedure TTestPLMConnectionParameter.Load(
  AConnectionParameter: TConnectionParameter);
begin
  FId := AConnectionParameter.Id;
  FName := AConnectionParameter.Name;
  FDefaultValue := AConnectionParameter.DefaultValue;
  FOrder := AConnectionParameter.Order;
end;

{ TTestPLMLoginParameters }

destructor TTestPLMLoginParameters.Destroy;
var
  i: Integer;
begin
  for i := Low(FParams) to High(FParams) do
    FParams[i] := nil;
  inherited;
end;

function TTestPLMLoginParameters.Get_Count: Integer;
begin
  Result := Length(FParams);
end;

function TTestPLMLoginParameters.Get_Items(Index: Integer): IPLMLoginParameter;
begin
  Result := FParams[Index];
end;

procedure TTestPLMLoginParameters.Load(ALogin: TLoginParameters;
  ALoginValues: TLoginParamValues);
var
  i: Integer;
  Parameter: TTestPLMLoginParameter;
begin
  SetLength(FParams, Length(ALogin));
  for i := Low(FParams) to High(FParams) do begin
    Parameter := TTestPLMLoginParameter.Create;
    Parameter.Load(ALogin[i]);
    if Parameter.FId = 'Locale' then
      Parameter.Set_LOV(ALoginValues);
    FParams[i] := Parameter;
  end;
end;

{ TTestPLMLoginParameter }

destructor TTestPLMLoginParameter.Destroy;
begin
  FLOV := nil;
  inherited;
end;

function TTestPLMLoginParameter.Get_DefaultValue: WideString;
begin
  Result := FDefaultValue;
end;

function TTestPLMLoginParameter.Get_Id: WideString;
begin
  Result := FId;
end;

function TTestPLMLoginParameter.Get_LOV: IPLMLoginParamListOfValues;
begin
  Result := FLOV;
end;

function TTestPLMLoginParameter.Get_Mandatory: WordBool;
begin
  Result := FMandatory;
end;

function TTestPLMLoginParameter.Get_Name: WideString;
begin
  Result := FName;
end;

function TTestPLMLoginParameter.Get_Order: Integer;
begin
  Result := FOrder;
end;

function TTestPLMLoginParameter.Get_Password: WordBool;
begin
  Result := FPassword;
end;

procedure TTestPLMLoginParameter.Load(ALoginParameter: TLoginParameter);
begin
  FId := ALoginParameter.Id;
  FName := ALoginParameter.Name;
  FDefaultValue := ALoginParameter.DefaultValue;
  FOrder := ALoginParameter.Order;
  FMandatory := ALoginParameter.Mandatory;
  FPassword := ALoginParameter.Password;
  FLOV := nil;
end;

procedure TTestPLMLoginParameter.Set_LOV(ALoginValues: TLoginParamValues);
var
  LOV: TTestPLMLoginParamListOfValues;
begin
  LOV := TTestPLMLoginParamListOfValues.Create;
  LOV.Load(ALoginValues);
  FLOV := LOV;
end;

{ TTestPLMLoginParamListOfValues }

destructor TTestPLMLoginParamListOfValues.Destroy;
var
  i: Integer;
begin
  for i := Low(FLOVs) to High(FLOVs) do
    FLOVs[i] := nil;
  inherited;
end;

function TTestPLMLoginParamListOfValues.Get_Count: Integer;
begin
  Result := Length(FLOVs);
end;

function TTestPLMLoginParamListOfValues.Get_Items(
  Index: Integer): IPLMLoginParamValue;
begin
  Result := FLOVs[Index];
end;

procedure TTestPLMLoginParamListOfValues.Load(ALoginValues: TLoginParamValues);
var
  i: Integer;
  Value: TTestPLMLoginParamValue;
begin
  SetLength(FLOVs, Length(ALoginValues));
  for i := Low(FLOVs) to High(FLOVs) do begin
    Value := TTestPLMLoginParamValue.Create;
    Value.Set_Value(ALoginValues[i]);
    FLOVs[i] := Value;
  end;
end;

{ TTestPLMLoginParamValue }

function TTestPLMLoginParamValue.Get_DisplayName: WideString;
begin
  Result := FDisplayName;
end;

function TTestPLMLoginParamValue.Get_Value: WideString;
begin
  Result := FValue;
end;

procedure TTestPLMLoginParamValue.Set_Value(ALoginValue: TLoginParamValue);
begin
  FDisplayName := ALoginValue.DisplayName;
  FValue := ALoginValue.Value;
end;

{ TTestPLMSettingsParameters }

destructor TTestPLMSettingsParameters.Destroy;
var
i : Integer;
begin
  for i := Low(FParams) to High(FParams) do
    FParams[i] := nil;
  inherited;
end;

function TTestPLMSettingsParameters.Get_Count: Integer;
begin
  Result := Length(FParams);
end;

function TTestPLMSettingsParameters.Get_Items(
  Index: Integer): IPLMSettingsParameter;
begin
  Result := FParams[Index];
end;

procedure TTestPLMSettingsParameters.Load(ASettings: TSettingsParameters);
var
  i: Integer;
  Parameter: TTestPLMSettingsParameter;
begin
  SetLength(FParams, Length(ASettings));
  for i := Low(FParams) to High(FParams) do begin
    Parameter := TTestPLMSettingsParameter.Create;
    Parameter.Load(ASettings[i]);
    FParams[i] := Parameter;
  end;
end;

{ TTestPLMSettingsParameter }

function TTestPLMSettingsParameter.Get_DefaultValue: WideString;
begin
  Result := FDefaultValue;
end;

function TTestPLMSettingsParameter.Get_Id: WideString;
begin
  Result := FId;
end;

function TTestPLMSettingsParameter.Get_Name: WideString;
begin
  Result := FName;
end;

function TTestPLMSettingsParameter.Get_Order: Integer;
begin
  Result := FOrder;
end;

procedure TTestPLMSettingsParameter.Load(
  ASettingsParameter: TSettingsParameter);
begin
  FId := ASettingsParameter.Id;
  FName := ASettingsParameter.Name;
  FDefaultValue := ASettingsParameter.DefaultValue;
  FOrder := ASettingsParameter.Order;
end;

{ TTestPLMProjectPreview }

function TTestPLMProjectPreview.Get_Height: Integer;
begin
  Result := 300;
end;

function TTestPLMProjectPreview.Get_Width: Integer;
begin
  Result := 400;
end;

end.
