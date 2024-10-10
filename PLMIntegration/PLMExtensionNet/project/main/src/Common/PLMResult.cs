using CAMAPI.Extension.PLM;

namespace PLMIntegrarionExamples.Common;

public class PLMResult : IPLMResult
{
    public int Code { get; set; }

    public string ErrorMessage { get; set; } = string.Empty;

    public string WarningMessage { get; set; } = string.Empty;

    public void SetSuccessful(string warningMsg = "")
    {
        Code = 0;
        ErrorMessage = string.Empty;
        WarningMessage = warningMsg;
    }   
}
