using System;
using System.IO;
using CAMAPI.Application;
using CAMAPI.CoordinateSystem;
using CAMAPI.CustomAttributes;
using CAMAPI.MCDFormerTypes;
using STTypes;


namespace ExtensionUtilityExportInformationNet
{
    ///
    public class ExportToolpathCommand : ICamApiExportToolpathCommand {}
    ///
    ///
    public class ExportToolpathCommand_Caption(ExportToolpathReceiver receiver) : ICamApiExportToolpathCommand_Caption
    {
        private readonly ExportToolpathReceiver _receiver = receiver;

        ///
        public void SetCaption(string Caption)
        {
            _receiver.SetCurrentCaption(Caption);
        }
    }
}