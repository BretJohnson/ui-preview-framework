using EcommerceMAUI.ViewModel;
using static EcommerceMAUI.Model.TrackOrderModel;

namespace EcommerceMAUI.Views;

public partial class TrackOrderView : ContentPage
{
    public TrackOrderView(Track data)
    {
        InitializeComponent();
        BindingContext = new TrackOrderViewModel(data);
    }
}
