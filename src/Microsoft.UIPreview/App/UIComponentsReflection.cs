﻿using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.UIPreview.App;

public class UIComponentsReflection : UIComponentsBase<UIComponentReflection, PreviewReflection>
{
    public void AddFromAssembly(Assembly assembly)
    {
        IEnumerable<UIComponentCategoryAttribute> uiComponentCategoryAttributes = assembly.GetCustomAttributes<UIComponentCategoryAttribute>();
        foreach (UIComponentCategoryAttribute uiComponentCategoryAttribute in uiComponentCategoryAttributes)
        {
            UIComponentCategory category = GetOrAddCategory(uiComponentCategoryAttribute.Name);

            foreach (Type type in uiComponentCategoryAttribute.UIComponentTypes)
            {
                UIComponentReflection component = GetOrAddComponent(type);
                component.InitCategory(category);
            }
        }

        Type[] types = assembly.GetExportedTypes();
        foreach (Type type in types)
        {
            PreviewAttribute? typePreviewAttribute = type.GetCustomAttribute<PreviewAttribute>(false);
            if (typePreviewAttribute != null)
            {
                AddPreview(new PreviewClassReflection(typePreviewAttribute, type));
            }

            MethodInfo[] methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly);
            foreach (MethodInfo method in methods)
            {
                PreviewAttribute? previewAttribute = method.GetCustomAttribute<PreviewAttribute>(false);

                if (previewAttribute != null)
                {
                    AddPreview(new UIPreviewStaticMethodReflection(previewAttribute, method));
                }
            }

            if (CanBeAutoGeneratedPreview(type))
            {
                UIComponentReflection? uiComponent = GetUIComponent(type.FullName);

                // If the type doesn't have any previews and this class can be an auto-generated preview, then add that
                if ((uiComponent == null || uiComponent.Previews.Count == 0) && CanBeAutoGeneratedPreview(type))
                {
                    uiComponent ??= GetOrAddComponent(type);

                    var preview = new PreviewClassReflection(type, isAutoGenerated: true);
                    AddPreview(preview);
                }
            }
        }
    }

    private static bool CanBeAutoGeneratedPreview(Type type)
    {
        if (type.IsAbstract)
        {
            return false;
        }

        bool derivesFromContentPage = false;
        Type baseType = type.BaseType;
        while (baseType != null)
        {
            if (baseType.FullName == "Xamarin.Forms.ContentPage" || baseType.FullName == "Microsoft.Maui.Controls.ContentPage")
            {
                derivesFromContentPage = true;
                break;
            }
            baseType = baseType.BaseType;
        }

        if (! derivesFromContentPage)
        {
            return false;
        }

        // Check for a public or internal default constructor
        ConstructorInfo constructor = type.GetConstructor(Type.EmptyTypes);
        if (constructor == null || !(constructor.IsPublic || constructor.IsAssembly))
        {
            return false;
        }

        return true;
    }

    public UIComponentReflection GetOrAddComponent(Type type)
    {
        string name = type.FullName;

        UIComponentReflection? component = GetUIComponent(name);
        if (component == null)
        {
            component = new UIComponentReflection(type, null);
            AddUIComponent(component);
        }

        return component;
    }

    public void AddPreview(PreviewReflection preview)
    {
        UIComponentReflection component = GetOrAddComponent(preview.UIComponentType);
        component.AddPreview(preview);
    }
}
