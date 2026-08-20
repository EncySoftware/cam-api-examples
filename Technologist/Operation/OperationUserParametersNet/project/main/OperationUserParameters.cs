using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.TechOperation;

namespace OperationUserParametersNet;

/// <summary>
/// Shared navigation to the user-parameters list of the currently selected operation.
/// </summary>
internal static class OperationUserParameters
{
    /// <summary>
    /// Fixed parameter name used by the "Add" and "Delete" buttons so they operate on the
    /// same parameter. Adding an existing name updates its value/comment instead of duplicating.
    /// </summary>
    public const string ExampleParamName = "ApiExampleParam";

    /// <summary>Value written by the "Add" button.</summary>
    public const string ExampleParamValue = "42";

    /// <summary>Comment written by the "Add" button.</summary>
    public const string ExampleParamComment = "Added by the CAM API user-parameters example";

    /// <summary>
    /// Navigates ActiveProject -> Technologist -> CurrentOperation and invokes
    /// <paramref name="action"/> with the operation's user-parameters list. All intermediate COM
    /// wrappers (and the list itself) are kept alive for the duration of the call and disposed
    /// afterwards, so the action must finish its work synchronously.
    /// </summary>
    /// <param name="applicationCom">Live application wrapper (owned by the caller).</param>
    /// <param name="action">
    /// Receives the live user-parameters list, or <c>null</c> when the current operation has no
    /// MCD template (a group, the root operation, or an operation type without a toolpath).
    /// </param>
    /// <exception cref="Exception">Thrown when no operation is selected in the technology tree.</exception>
    public static void WithUserParameters(
        ComWrapper<ICamApiApplication> applicationCom,
        Action<ComWrapper<ICamApiUserParametersList>?> action)
    {
        // ENCY always has an active project, so no null check is needed here.
        using var projectCom = applicationCom.GetActiveProject();
        using var technologistCom = projectCom.Technologist();
        using var operationCom = technologistCom.CurrentOperation();
        if (operationCom.IsNull)
            throw new Exception("No operation is selected in the technology tree.");

        // GetUserParameters returns a nil wrapper (not an error) for operations without an
        // MCD template. Note: a parameter Name may be a postprocessor macro expression that
        // ENCY evaluates per node when generating the NC program; the human-readable label
        // is the Comment.
        using var listCom = operationCom.GetUserParameters();
        action(listCom.IsNull ? null : listCom);
    }
}
