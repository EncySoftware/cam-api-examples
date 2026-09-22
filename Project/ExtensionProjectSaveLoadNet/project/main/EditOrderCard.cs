using System.Runtime.InteropServices;
using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.ResultStatus;
using CAMAPI.UIDialogs;
using CAMAPI.UIDialogs.DotnetHelper;

namespace ExtensionProjectSaveLoadNet;

[ComVisible(true), ClassInterface(ClassInterfaceType.None)]
public sealed class EditOrderCard : IExtension, IExtensionUtility
{
    public const string ExtensionId = "Examples.ProjectSaveLoad.EditOrderCard";
    public IExtensionInfo? Info { get; set; }

    public void Run(IExtensionUtilityContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;
        try
        {
            using var utilityContext = ComWrapper.Create(context);
            using var application = utilityContext.InvokeAndWrap(c => c.CamApplication);
            using var project = application.GetActiveProject();
            var documentId = ProjectHelper.Id(project);
            if (!OrderCardStore.IsCurrent(documentId))
                throw new InvalidOperationException("Open or create a project after enabling the project serializer.");

            Edit(OrderCardStore.Current);
        }
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = $"{ExtensionId}.Run: {e.Message}";
        }
    }

    internal static void Edit(OrderCardState state)
    {
        if (state.LoadError is { } error)
            throw new InvalidOperationException(error);

        // Work with a draft so Cancel leaves the document unchanged.
        var draft = state.Current;
        using var properties = new SimplePropIterator();
        properties.AddStringProp("Order number", () => draft.OrderNumber,
            value => draft = draft with { OrderNumber = value });
        properties.AddStringProp("Customer", () => draft.Customer,
            value => draft = draft with { Customer = value });
        properties.AddStringProp("Notes", () => draft.Notes,
            value => draft = draft with { Notes = value });

        using var window = new CamApiInspectorWindow { Caption = "Project order card (save the project after editing)" };
        window.SetPropIterator(properties);
        window.SetButtons(MessageBoxHelper.BuildButtons(TUIButtonType.btOk, TUIButtonType.btCancel));
        if (window.Show() == TUIButtonType.btOk)
            state.Current = draft;
    }
}
