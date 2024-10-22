unit Main;

interface

uses
  System.SysUtils,
  IDL.CAMAPI.Extensions,
  TestPLMInterface;

function CreateInstanceOfExtension(PluginID: WideString): NativeUInt; stdcall;

implementation

function CreateInstanceOfExtension(PluginID: WideString): NativeUInt;
begin
  var res: IExtension;

  if SameText(PluginID, 'PLM.Extension.Delphi') then
    res := TTestPLMInterface.Create() as IExtension

  else
    raise Exception.Create('Unknown ID ' + PluginID);

  res._AddRef;
  result := NativeUInt(res);
end;

exports
  CreateInstanceOfExtension;

end.
