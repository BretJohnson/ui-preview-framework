﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.UIPreview.App;

public class UIComponentsManagerReflection : UIComponentsManagerBase<UIComponentReflection, PreviewReflection>
{
    private static readonly Lazy<UIComponentsManagerReflection> s_instance = new Lazy<UIComponentsManagerReflection>(() => new UIComponentsManagerReflection());

    public static UIComponentsManagerReflection Instance => s_instance.Value;

    private UIComponentsManagerReflection()
    {
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(p => !p.IsDynamic).ToArray();

        foreach (Assembly assembly in assemblies)
        {
            AddUIComponentBaseClassesFromAssembly(assembly);
        }

        foreach (Assembly assembly in assemblies)
        {
            AddUIComponentsFromAssembly(assembly);
        }
    }

    public void AddUIComponentBaseClassesFromAssembly(Assembly assembly)
    {
        foreach (PageUIComponentBaseTypeAttribute pageUIComponentAttribute in assembly.GetCustomAttributes<PageUIComponentBaseTypeAttribute>())
        {
            _pageUIComponentBaseTypes.AddBaseType(pageUIComponentAttribute.Platform, pageUIComponentAttribute.BaseType);
        }

        foreach (ControlUIComponentBaseTypeAttribute controlUIComponentAttribute in assembly.GetCustomAttributes<ControlUIComponentBaseTypeAttribute>())
        {
            _controlUIComponentBaseTypes.AddBaseType(controlUIComponentAttribute.Platform, controlUIComponentAttribute.BaseType);
        }
    }

    public void AddUIComponentsFromAssembly(Assembly assembly)
    {
        IEnumerable<UIComponentCategoryAttribute> uiComponentCategoryAttributes = assembly.GetCustomAttributes<UIComponentCategoryAttribute>();
        foreach (UIComponentCategoryAttribute uiComponentCategoryAttribute in uiComponentCategoryAttributes)
        {
            UIComponentCategory category = GetOrAddCategory(uiComponentCategoryAttribute.Name);

            foreach (Type type in uiComponentCategoryAttribute.UIComponentTypes)
            {
                UIComponentReflection component = GetOrAddUIComponent(type);
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
                    uiComponent ??= GetOrAddUIComponent(type);

                    var preview = new PreviewClassReflection(type, isAutoGenerated: true);
                    AddPreview(preview);
                }
            }
        }
    }

    private bool CanBeAutoGeneratedPreview(Type type)
    {
        if (type.IsAbstract)
        {
            return false;
        }

        bool derivesFromUIComponentBaseType = false;
        Type baseType = type.BaseType;
        while (baseType != null)
        {
            if (IsUIComponentBaseType(baseType.FullName, out _, out _))
            {
                derivesFromUIComponentBaseType = true;
                break;
            }

            baseType = baseType.BaseType;
        }

        if (! derivesFromUIComponentBaseType)
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

    public UIComponentReflection GetOrAddUIComponent(Type type)
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
        UIComponentReflection component = GetOrAddUIComponent(preview.UIComponentType);
        component.AddPreview(preview);
    }
}
