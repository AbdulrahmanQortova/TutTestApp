using Tut.PageModels.Popups;
namespace Tut.Popups;

public partial class ArrivedPopup
{
	public ArrivedPopup(ArrivedPopupModel model)
	{
		InitializeComponent();
        BindingContext = model;
    }
	
}
