namespace TutMauiCommon.Services;

public class ShellService : IShellService
{
    public Task DisplayAlertAsync(string title, string message, string cancel)
    {
        return Shell.Current.DisplayAlertAsync(title, message, cancel);
    }
    public Task<bool> DisplayConfirmAsync(string title, string message, string accept, string cancel)
    {
        return Shell.Current.DisplayAlertAsync(title, message, accept, cancel);
    }
}
