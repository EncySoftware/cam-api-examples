using System;
using System.IO;
using CAMAPI.Application;
using CAMAPI.CoordinateSystem;
using CAMAPI.CustomAttributes;
using CAMAPI.MCDFormerTypes;
using STTypes;
using Geometry.VecMatrLib;


namespace ExtensionUtilityExportInformationNet
{
    ///
    public class ExportToolpathReceiver(JsonBuilder builder, bool treeOutput = false) : 
        ICamApiExportToolpathReceiver,
        ICamApiExportToolpathCommand,
        ICamApiExportToolpathCommand_Feedrate
    {
        ///
        private readonly JsonBuilder _builder = builder;
        ///
        private readonly bool _treeOutput = treeOutput;
        T3DMatrix _pointsCS;
        private TST3DPoint? _currentVZ;
        private TST3DPoint? _currentVX;
        private int _currentFeedType;
        ///
        public void OpenCommand(int CommandCode, ulong CommandHandle, ulong ParentCommandHandle)
        {
            _builder.BeginObject();
            _builder.AddStrPair("CommandCode", CommandCode.ToString());
            _builder.AddStrPair("CommandHandle", CommandHandle.ToString());
            _builder.AddStrPair("ParentCommandHandle", ParentCommandHandle.ToString());
        }
        ///
        public void CloseCommand()
        { 
            if (_treeOutput) _builder.EndObject();
        }
        ///
        public void OpenCommandData()  { }
        ///
        public void CloseCommandData() 
        {
            if (!_treeOutput) _builder.EndObject();
        }
        ///
        public void BeginChildren()
        {
            if (_treeOutput) _builder.BeginArray("Children");
        }
        ///
        public void EndChildren()
        {
            if (_treeOutput) _builder.EndArray();
        }
        ///
        public void BeginPoints(TST3DMatrix pointsCS)
        {
            _currentVZ = null;
            _currentVX = null;
            _pointsCS = pointsCS; //T3DMatrix.Unit
            _builder.BeginArray("Points");
        }
        ///
        public void EndPoints() => _builder.EndArray();
        ///
        public void SetNormal(TST3DPoint vZ)
        {
            _currentVZ = _pointsCS.TransformVector(vZ);
            _currentVX = null;
        }
        ///
        public void SetOrientation(TST3DPoint vZ, TST3DPoint vX)
        {
            _currentVZ =  _pointsCS.TransformVector(vZ);
            _currentVX =  _pointsCS.TransformVector(vX);
        } 
        ///
        public void AddPoint(TST3DPoint point)
        {
            _builder.BeginObject();
            var localPnt =  _pointsCS.TransformPoint(point);
            GeometrySaveHelper.ShowPointData(localPnt, "EndPoint", _builder);
            if (_currentVZ.HasValue)
                GeometrySaveHelper.ShowPointData(_currentVZ.Value, "vZ", _builder);
            if (_currentVX.HasValue)
                GeometrySaveHelper.ShowPointData(_currentVX.Value, "vX", _builder);
            _builder.EndObject();
        }
        ///
        public ICamApiExportToolpathCommand? GetCommandReceiver(int CommandCode) 
        {       
            switch (CommandCode) {
                case (int)TCLDataCommandCode.cmdFedrat:
                case (int)TCLDataCommandCode.cmdRapid:
                case (int)TCLDataCommandCode.cmdPhysicGoto:
                case (int)TCLDataCommandCode.cmdMultiGoto:
                    return this;
                default: 
                    return null;
            }
        }
        
        ///
        public void SetCurrentCaption(string caption) => _builder.AddStrPair("CommandCaption", caption);

        ///
        public ICamApiExportToolpathCommand_Caption GetCaptionReceiver() => new ExportToolpathCommand_Caption(this);

        void ICamApiExportToolpathCommand_Feedrate.SetFeed(int FeedType, TCamApiFeedUnits FeedUnits, double FeedValue)
        {
            _currentFeedType = FeedType;
            _builder.AddIntPair("FeedType", FeedType);
        }
    }
}