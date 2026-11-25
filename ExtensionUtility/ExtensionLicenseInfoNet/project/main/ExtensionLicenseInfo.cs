using System.Text;
using CAMAPI.Application;
using CAMAPI.Extensions;
using CAMAPI.ResultStatus;
using CAMAPI.DotnetHelper;
using CAMAPI.UIDialogs;
using CAMAPI.UIDialogs.DotnetHelper;

namespace ExtensionLicenseInfoNet;

/// <summary>
/// Utility that greets the user by the name of the ENCY account and shows the license ENCY runs under
/// </summary>
public class ExtensionLicenseInfo : IExtension, IExtensionUtility
{
    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

    /// <summary>
    /// Reads the account and the license and shows them in a message box. Errors return in resultStatus, an
    /// exception never leaves the method
    /// </summary>
    /// <param name="context">Information about current running instance</param>
    /// <param name="resultStatus">Structure to return error</param>
    public void Run(IExtensionUtilityContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;
        try
        {
            string card;
            using (var applicationCom = ComWrapper.Create(context.CamApplication))
                card = Greeting(applicationCom) + Environment.NewLine + Environment.NewLine + License(applicationCom);
            ShowCard(card);
        }
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }

    /// <summary>
    /// Greeting by the name of the account ENCY is signed in with
    /// </summary>
    private static string Greeting(ComWrapper<ICamApiApplication> applicationCom)
    {
        using var accountCom = applicationCom.UserAccount();

        // Blocks for up to 15 seconds while the access token comes from the server
        var displayName = accountCom.DisplayName();
        return string.IsNullOrEmpty(displayName)
            ? "Hello! You are not signed in to an ENCY account."
            : $"Hello, {displayName}!";
    }

    /// <summary>
    /// Edition, trial flag, expiration date and days left of the license ENCY runs under
    /// </summary>
    private static string License(ComWrapper<ICamApiApplication> applicationCom)
    {
        using var licenseCom = applicationCom.LicenseInfo();

        var edition = licenseCom.Edition();
        var isTrial = licenseCom.IsTrial();
        var expirationDate = licenseCom.ExpirationDate();
        var daysLeft = licenseCom.DaysLeft();

        var text = new StringBuilder();
        text.AppendLine($"Edition: {(string.IsNullOrEmpty(edition) ? "no valid license" : edition)}");
        text.AppendLine($"Trial: {(isTrial ? "yes" : "no")}");
        text.AppendLine($"Expires: {(string.IsNullOrEmpty(expirationDate) ? "never, perpetual license" : expirationDate)}");
        text.Append(daysLeft < 0
            ? "Days left: no time limit, or the server has not answered yet"
            : $"Days left: {daysLeft}");
        return text.ToString();
    }

    /// <summary>
    /// Shows the card in an ENCY message box
    /// </summary>
    private static void ShowCard(string card)
    {
        using var helperCom = UIDialogs.CreateHelper();
        var buttons = MessageBoxHelper.BuildButtons(TUIButtonType.btOk);
        helperCom.Invoke(helper =>
        {
            helper.MessageBox(card, TMessageDialogType.mdtInformation, buttons, TUIButtonType.btOk, "License and account");
        });
    }
}
