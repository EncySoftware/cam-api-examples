unit Main;

interface

uses
  System.SysUtils
  ,IDL.CAMAPI.Extensions
  ,Extension
;


function CreateInstanceOfExtension(PluginID: WideString): NativeUInt; stdcall;

implementation

function CreateInstanceOfExtension(PluginID: WideString): NativeUInt;
begin
  var res: IExtension;

  if SameText(PluginID, 'Extension.Utility.Empty.Delphi') then
    res := TExtensionTest.Create() as IExtension

  else
    raise Exception.Create('Unknown ID ' + PluginID);

  res._AddRef;
  result := NativeUInt(res);
end;

exports
  CreateInstanceOfExtension;

end.

