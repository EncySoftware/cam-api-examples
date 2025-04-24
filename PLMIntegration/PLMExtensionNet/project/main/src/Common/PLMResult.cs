using CAMAPI.Extension.PLM;

namespace PLMIntegrarionExamples.Common;

/// <summary>
/// Represents the result of an operation within the PLM extension.
/// </summary>
public class PLMResult : IPLMResult
{
    /// <summary>
    /// Gets or sets the result code of the operation.
    /// </summary>
    public int Code { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the warning message.
    /// </summary>
    public string WarningMessage { get; set; } = string.Empty;

    /// <summary>
    /// Marks the result as successful and optionally sets a warning message.
    /// </summary>
    public void SetSuccessful(string warningMsg = "")
    {
        Code = 0;
        ErrorMessage = string.Empty;
        WarningMessage = warningMsg;
    }   
}
