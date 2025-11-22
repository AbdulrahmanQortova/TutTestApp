namespace TutMauiCommon.Services;

public interface IShellService
{
    Task DisplayAlertAsync(string title, string message, string cancel);
    Task<bool> DisplayConfirmAsync(string title, string message, string accept, string cancel);
}
