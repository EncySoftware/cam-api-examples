using CAMAPI.Extension.PLM;

namespace PLMIntegrarionExamples.Items;

public class PLMFiles : IPLMFiles
{
    public int Count => filePaths.Count;

    public string this[int Index] => filePaths[Index];

    public PLMFiles()
    {
        filePaths = new List<string>();
    }

    private List<string> filePaths;

    public void AddFiles(List<string> newFilePaths)
    {
        filePaths.AddRange(newFilePaths);
    }    
}
