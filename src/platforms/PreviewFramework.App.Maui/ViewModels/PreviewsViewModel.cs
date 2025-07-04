using System;
using System.Collections.Generic;
using PreviewFramework.SharedModel;
using PreviewFramework.SharedModel.App;

namespace PreviewFramework.App.Maui.ViewModels;

public class PreviewsViewModel // : INotifyPropertyChanged
{
    private static readonly Lazy<PreviewsViewModel> s_lazyInstance = new Lazy<PreviewsViewModel>(() => new PreviewsViewModel());

    public static PreviewsViewModel Instance => s_lazyInstance.Value;

    public IReadOnlyList<PreviewsItemViewModel> PreviewsItems { get; }
    public bool HasCategories { get; }

    private PreviewsViewModel()
    {
        UIComponentsManagerReflection uiComponentsManager = MauiPreviewApplication.Instance.GetUIComponentsManager();
        var previewsItems = new List<PreviewsItemViewModel>();
        HasCategories = uiComponentsManager.HasCategories;

        void AddComponentPreviews(UIComponentReflection uiComponent)
        {
            previewsItems.Add(new UIComponentViewModel(uiComponent));
            if (uiComponent.HasMultiplePreviews)
            {
                foreach (PreviewReflection preview in uiComponent.Previews)
                {
                    previewsItems.Add(new PreviewViewModel(uiComponent, preview));
                }
            }
        }

        if (HasCategories)
        {
            IReadOnlyList<(UIComponentCategory Category, IReadOnlyList<UIComponentReflection> UIComponents)> categorizedUIComponents = uiComponentsManager.CategorizedUIComponents;
            foreach ((UIComponentCategory category, IReadOnlyList<UIComponentReflection> uiComponents) in categorizedUIComponents)
            {
                previewsItems.Add(new UIComponentCategoryViewModel(category));

                foreach (UIComponentReflection uiComponent in uiComponents)
                {
                    AddComponentPreviews(uiComponent);
                }
            }
        }
        else
        {
            foreach (UIComponentReflection uiComponent in uiComponentsManager.SortedUIComponents)
            {
                AddComponentPreviews(uiComponent);
            }
        }

        PreviewsItems = previewsItems;
    }

    public void NavigateToPreview(UIComponentReflection uiComponent, PreviewReflection preview)
    {
        MauiPreviewApplication.Instance.PreviewNavigatorService.NavigateToPreview(uiComponent, preview);
    }
}
