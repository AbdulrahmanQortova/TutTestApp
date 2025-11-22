using CommunityToolkit.Mvvm.ComponentModel;
namespace Tut.PageModels.Popups;

public partial class ArrivedPopupModel : ObservableObject, IQueryAttributable
{
    [ObservableProperty]
    private string _icon = string.Empty;
    [ObservableProperty]
    private string _title = string.Empty;
    [ObservableProperty]
    private string _message = string.Empty;
    [ObservableProperty]
    private string _money = string.Empty;
    [ObservableProperty]
    private string _currency = string.Empty;


    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(nameof(Icon), out var icon))
        {
            Icon = icon as string ?? string.Empty;
        }
        if (query.TryGetValue(nameof(Title), out var title))
        {
            Title = title as string ?? string.Empty;
        }
        if (query.TryGetValue(nameof(Message), out var message))
        {
            Message = message as string ?? string.Empty;
        }
        if (query.TryGetValue(nameof(Money), out var money))
        {
            Money = money as string ?? string.Empty;
        }
        if (query.TryGetValue(nameof(Currency), out var currency))
        {
            Currency = currency as string ?? string.Empty;
        }
    }
}
