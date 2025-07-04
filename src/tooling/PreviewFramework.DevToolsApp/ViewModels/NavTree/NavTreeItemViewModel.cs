using Microsoft.UI.Xaml.Data;
using PreviewFramework.DevToolsApp.Utilities;
using PreviewFramework.SharedModel;

namespace PreviewFramework.DevToolsApp.ViewModels;

[Bindable]
public abstract partial class NavTreeItemViewModel : ObservableObject
{
    public NavTreeItemViewModel()
    {
    }

    public string Content => DisplayName;

    public abstract string DisplayName { get; }
    public abstract string PathIcon { get; }
    public virtual IReadOnlyList<NavTreeItemViewModel>? Children { get; }

    /// <summary>
    /// Called when this item is invoked (selected) in the TreeView.
    /// Override this method in derived classes to implement navigation or other actions.
    /// </summary>
    public virtual void OnItemInvoked()
    {
    }
}
