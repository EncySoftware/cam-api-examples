using PLMIntegrarionExamples.Items;
using PLMIntegrarionExamples.Parameters;
using PLMIntegrarionExamples.Common;
using PLMIntegrarionExamples.Tree;
using CAMAPI.Extensions;
using CAMAPI.Extension.PLM;

namespace PLMIntegrarionExamples;

class PLMExtensionNet : IExtensionPLM, IExtension
{
    public PLMExtensionNet()
    {
        InitParameters();
    }

    public IExtensionInfo Info { get; set; }

    private const string PLMExtensionTempDir = "..\\CAMAPIPLMExtensionData";

    private const string sppDataItemName = "Fanuc (30i)_Mill";
    private const string sppFilePath = PLMExtensionTempDir + "\\Fanuc (30i)_Mill.sppx";
    private const string modelDataItemName = "49-1 Test";
    private const string modelFilePath = PLMExtensionTempDir + "\\49-1.igs";
    private const string machineDataItemName = "DMU60 Test";
    private const string machineFilePath = PLMExtensionTempDir + "\\DMU60.zip";
    private const string toolDataItemName = "Drill Test";
    private const string toolFilePath = PLMExtensionTempDir + "\\Drill.sc12tool";
    
    private List<TempParam> connectionParams;
    private List<TempLoginParam> loginParams;
    private List<TempParamValue>? loginParamValues = null;
    private List<TempParam> settingsParams;

    public void SetLanguage(uint LanguageID, byte CodePage)
    {
        // setting language
    }

    public IPLMParameters GetParameters() => new PLMParameters(connectionParams, loginParams, loginParamValues, settingsParams);

    public IPLMResult Connect(IPLMParameterValues Values) => ReturnSuccessfulResult();

    public IPLMResult Disconnect() => ReturnSuccessfulResult();

    public IPLMResult Install() => ReturnSuccessfulResult();

    public IPLMResult Uninstall() => ReturnSuccessfulResult();

    public IPLMResult GetItem(TPLMItemType ItemType, string ItemId, out IPLMTree Items)
    {
        Items = new PLMTree();
        return ReturnSuccessfulResult();
    }

    public IPLMResult GetLinkedItem(TPLMItemType ItemType, TPLMItemType LinkedItemType, string ItemId, out IPLMTree Items)
    {
        if(LinkedItemType == TPLMItemType.itPostprocessor)
            Items = new PLMTree(TPLMItemType.itPostprocessor);
        else
            Items = new PLMTree();
        return ReturnSuccessfulResult();
    }

    public IPLMResult GetChilds(TPLMItemType ItemType, string ParentItemId, out IPLMTree Items)
    {
        Items = new PLMTree();
        return ReturnSuccessfulResult();
    }

    public IPLMResult FindItems(TPLMItemType ItemType, string ItemName, out IPLMTree Items)
    {
        Items = new PLMTree();
        return ReturnSuccessfulResult();
    }

    public IPLMResult DownloadItems(IPLMItems Items, string FilePath, out IPLMDataItems DwnItems)
    {
        var tempItems = new List<TempItem>();

        for (var i = 0; i < Items.Count; i++)
            switch(Items[i].Type)
            {
                case TPLMItemType.itNone:
                break;
                case TPLMItemType.itModel:
                var tempItem = new TempItem {
                    Id = modelDataItemName,
                    Name = modelDataItemName,
                    ItemType = Items[i].Type,
                    TimeStamp = ""
                };
                tempItem.FilePaths.Add(modelFilePath);
                tempItems.Add(tempItem);
                break;
                case TPLMItemType.itWorkpiece:
                tempItem = new TempItem {
                    Id = modelDataItemName,
                    Name = modelDataItemName,
                    ItemType = Items[i].Type,
                    TimeStamp = ""
                };
                tempItem.FilePaths.Add(modelFilePath);
                tempItems.Add(tempItem);
                break;
                case TPLMItemType.itTool:
                tempItem = new TempItem {
                    Id = toolDataItemName,
                    Name = toolDataItemName,
                    ItemType = Items[i].Type,
                    TimeStamp = ""
                };
                tempItem.FilePaths.Add(toolFilePath);
                tempItems.Add(tempItem);
                break;
                case TPLMItemType.itMachine:
                tempItem = new TempItem {
                    Id = machineDataItemName,
                    Name = machineDataItemName,
                    ItemType = Items[i].Type,
                    TimeStamp = ""
                };
                tempItem.FilePaths.Add(machineFilePath);
                tempItems.Add(tempItem);
                break;
                case TPLMItemType.itProject:
                break;
                case TPLMItemType.itPostprocessor:
                tempItem = new TempItem {
                    Id = sppDataItemName,
                    Name = sppDataItemName,
                    ItemType = Items[i].Type,
                    TimeStamp = ""
                };
                tempItem.FilePaths.Add(sppFilePath);
                tempItems.Add(tempItem);
                break;                
            }
        
        DwnItems = new PLMDataItems(tempItems);
        return ReturnSuccessfulResult();
    }

    public IPLMResult DownloadProject(string ItemId, string FilePath, out IPLMDataItems DwnItems, out IPLMProjectStructItems PrjStructItems)
    {
        DwnItems = new PLMDataItems();
        PrjStructItems = new PLMProjectStructItems();
        return ReturnSuccessfulResult();
    }

    public IPLMResult UploadProject(IPLMCAMProject Project, bool SaveAs, bool Replace) => ReturnSuccessfulResult();

    public IPLMResult UploadItem(TPLMItemType ItemType, string ItemId, IPLMFiles Files, bool Replace, out IPLMDataItems UplItems)
    {
        for(var i = 0; i < Files.Count; i++)
            File.Copy(Files[i], Path.Combine(PLMExtensionTempDir, Path.GetFileName(Files[i])));
        UplItems = new PLMDataItems();
        return ReturnSuccessfulResult();
    }

    private IPLMResult ReturnSuccessfulResult()
    {
        var result = new PLMResult();
        result.SetSuccessful();
        return result;
    }

    private void InitParameters()
    {
        connectionParams = new List<TempParam>
        {
            new() {
                Id = "Host",
                Name = "Host",
                DefaultValue = "myhost",
                Order = 0
            },
            new() {
                Id = "Port",
                Name = "Port",
                DefaultValue = "7001",
                Order = 1
            }
        };

        loginParams = new List<TempLoginParam>
        {
            new() {
                Id = "Login",
                Name = "Login",
                Mandatory = true,
                Password = false,
                DefaultValue = string.Empty,
                Order = 0
            },
            new() {
                Id = "Password",
                Name = "Password",
                Mandatory = true,
                Password = true,
                DefaultValue = string.Empty,
                Order = 1
            }
        };

        settingsParams = new List<TempParam>();
    }
}
