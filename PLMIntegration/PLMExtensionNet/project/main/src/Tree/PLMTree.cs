using CAMAPI.Extension.PLM;

namespace PLMIntegrarionExamples.Tree;

public class PLMTree : IPLMTree
{
    public int Count => treeItems.Count();

    public IPLMTreeItem this[int Index] => treeItems[Index];

    public PLMTree(TPLMItemType itemType = TPLMItemType.itNone)
    {
        treeItems = new List<PLMTreeItem>();
        
        switch (itemType)
        {            
            case TPLMItemType.itNone: 
                InitTestTreeItems();
                break;
            case TPLMItemType.itModel: 
                CreateTreeItem("ChildModelId", "ChildModelName", "Child Model PLMTree Item", TPLMItemType.itModel);
                break;
            case TPLMItemType.itWorkpiece: 
                CreateTreeItem("ChildWorkpieceId", "ChildWorkpieceName", "Child Workpiece PLMTree Item", TPLMItemType.itWorkpiece);
                break;
            case TPLMItemType.itTool: 
                CreateTreeItem("ChildToolId", "ChildToolName", "Child Tool PLMTree Item", TPLMItemType.itTool);
                break;
            case TPLMItemType.itMachine: 
                CreateTreeItem("ChildMachineId", "ChildMachineName", "Child Machine PLMTree Item", TPLMItemType.itMachine);
                break;
            case TPLMItemType.itProject: 
                CreateTreeItem("ChildProjectId", "ChildProjectName", "Child Project PLMTree Item", TPLMItemType.itProject);
                break;
            case TPLMItemType.itPostprocessor: 
                CreateTreeItem("ChildPostprocessorId", "ChildPostprocessorName", "Child Postprocessor PLMTree Item", TPLMItemType.itPostprocessor);
                break;                              
        }
    }

    private List<PLMTreeItem> treeItems;

    private void CreateTreeItem(string id, string name, string comment, TPLMItemType itemType) => treeItems.Add(new PLMTreeItem
    {
        Id = id,
        Name = name,
        Type = itemType,
        Comment = comment
    });

    private void InitTestTreeItems()
    {
        CreateTreeItem("NoneId", "NoneName", "Test None PLMTree Item", TPLMItemType.itNone);
        CreateTreeItem("ModelId", "ModelName", "Test Model PLMTree Item", TPLMItemType.itModel);
        CreateTreeItem("WorkpieceId", "WorkpieceName", "Test Workpiece PLMTree Item", TPLMItemType.itWorkpiece);
        CreateTreeItem("ToolId", "ToolName", "Test Tool PLMTree Item", TPLMItemType.itTool);
        CreateTreeItem("MachineId", "MachineName", "Test Machine PLMTree Item", TPLMItemType.itMachine);
        CreateTreeItem("ProjectId", "ProjectName", "Test Project PLMTree Item", TPLMItemType.itProject);
        CreateTreeItem("PostprocessorId", "PostprocessorName", "Test Postprocessor PLMTree Item", TPLMItemType.itPostprocessor);
    }

}
