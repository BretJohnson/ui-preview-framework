using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PreviewFramework.SharedModel.App;

public class GetUIComponentsViaReflection : UIComponentsManagerBuilderBase<UIComponentReflection, PreviewReflection>
{
    private readonly IServiceProvider? _serviceProvider;
    private readonly IUIComponentExclusionFilter? _exclusionFilter;

    /// <summary>
    /// Initializes a new instance of GetUIComponentsViaReflection and processes assemblies to discover UI components via reflection.
    /// </summary>
    /// <param name="serviceProvider">An optional IServiceProvider instance for dependency injection</param>
    /// <param name="additionalAppAssemblies">Additional app assembly names to scan</param>
    /// <param name="exclusionFilter">Optional filter to exclude certain types</param>
    public GetUIComponentsViaReflection(IServiceProvider? serviceProvider, IEnumerable<string> additionalAppAssemblies,
        IUIComponentExclusionFilter? exclusionFilter)
    {
        _serviceProvider = serviceProvider;
        _exclusionFilter = exclusionFilter;

        IEnumerable<Assembly> assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(p => !p.IsDynamic);
        foreach (Assembly assembly in assemblies)
        {
            AddUIComponentBaseClassesFromAssembly(assembly);
        }

        Assembly? entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly is not null)
        {
            AddUIComponentsFromAssembly(entryAssembly);
        }

        HashSet<string> additionalAppAssemblesSet = new(additionalAppAssemblies, StringComparer.OrdinalIgnoreCase);
        foreach (Assembly assembly in assemblies)
        {
            string? name = assembly.GetName().Name;
            if (name is not null && additionalAppAssemblesSet.Contains(name))
            {
                AddUIComponentsFromAssembly(assembly);
            }
        }
    }

    /// <summary>
    /// Creates an immutable UIComponentsManagerReflection from the builder's current state.
    /// </summary>
    /// <returns>An immutable UIComponentsManagerReflection containing all the builder's data</returns>
    public UIComponentsManagerReflection ToImmutable()
    {
        Validate();

        return new UIComponentsManagerReflection(UIComponentsByName, Categories);
    }

    private void AddUIComponentBaseClassesFromAssembly(Assembly assembly)
    {
        foreach (PageUIComponentBaseTypeAttribute pageUIComponentAttribute in assembly.GetCustomAttributes<PageUIComponentBaseTypeAttribute>())
        {
            AddUIComponentBaseType(UIComponentKind.Page, pageUIComponentAttribute.Platform, pageUIComponentAttribute.BaseType);
        }

        foreach (ControlUIComponentBaseTypeAttribute controlUIComponentAttribute in assembly.GetCustomAttributes<ControlUIComponentBaseTypeAttribute>())
        {
            AddUIComponentBaseType(UIComponentKind.Control, controlUIComponentAttribute.Platform, controlUIComponentAttribute.BaseType);
        }
    }

    private void AddUIComponentsFromAssembly(Assembly assembly)
    {
        IEnumerable<UIComponentCategoryAttribute> uiComponentCategoryAttributes = assembly.GetCustomAttributes<UIComponentCategoryAttribute>();
        foreach (UIComponentCategoryAttribute uiComponentCategoryAttribute in uiComponentCategoryAttributes)
        {
            IReadOnlyList<string> uiComponentTypeNames = uiComponentCategoryAttribute.UIComponentTypes.Select(t => t.FullName!).ToList();
            AddOrUpdateCategory(uiComponentCategoryAttribute.Name, uiComponentTypeNames);
        }

        Type[] types = assembly.GetExportedTypes();
        foreach (Type type in types)
        {
            if (_exclusionFilter?.ExcludeType(type) == true)
            {
                continue;
            }

            // Check for UIComponentAttribute
            UIComponentAttribute? uiComponentAttribute = type.GetCustomAttribute<UIComponentAttribute>(false);
            if (uiComponentAttribute is not null)
            {
                UIComponentKind kind = InferUIComponentKind(type);

                // Add or update the UI component with the display name from the attribute
                string uiComponentName = type.FullName ?? throw new ArgumentException("UI Component type must have a FullName");

                UIComponentReflection component;
                if (_uiComponentsByName.TryGetValue(uiComponentName, out UIComponentReflection? existingComponent))
                {
                    // Update the display name if the component already exists
                    component = new UIComponentReflection(type, existingComponent.Kind,
                        uiComponentAttribute.DisplayName, existingComponent.Previews);
                }
                else
                {
                    // Create a new component with no previews yet
                    component = new UIComponentReflection(type, kind, uiComponentAttribute.DisplayName, []);
                }

                _uiComponentsByName[uiComponentName] = component;
            }

            PreviewAttribute? typePreviewAttribute = type.GetCustomAttribute<PreviewAttribute>(false);
            if (typePreviewAttribute is not null)
            {
                AddPreview(new PreviewClassReflection(typePreviewAttribute, type));
            }

            MethodInfo[] methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly);
            foreach (MethodInfo method in methods)
            {
                PreviewAttribute? previewAttribute = method.GetCustomAttribute<PreviewAttribute>(false);

                if (previewAttribute is not null)
                {
                    AddPreview(new PreviewStaticMethodReflection(previewAttribute, method));
                }
            }

            if (CanBeAutoGeneratedPreview(type))
            {
                UIComponentReflection? uiComponent = GetUIComponent(type.FullName);

                // If the type doesn't have any previews and this class can be an auto-generated preview, then add that
                if ((uiComponent is null || uiComponent.Previews.Count == 0) && CanBeAutoGeneratedPreview(type))
                {
                    AddPreview(new PreviewClassReflection(type, isAutoGenerated: true));
                }
            }
        }
    }

    private UIComponentKind InferUIComponentKind(Type type)
    {
        Type? baseType = type.BaseType;
        while (baseType != null)
        {
            if (IsUIComponentBaseType(baseType.FullName, out UIComponentKind kind))
            {
                return kind;
            }

            baseType = baseType.BaseType;
        }

        return UIComponentKind.Unknown;
    }

    private bool CanBeAutoGeneratedPreview(Type type)
    {
        if (type.IsAbstract)
        {
            return false;
        }

        UIComponentKind kind = InferUIComponentKind(type);

        if (kind == UIComponentKind.Unknown)
        {
            return false;
        }

        // Abstract classes and interfaces cannot be directly instantiated. Also don't try to auto
        // create examples for non-public types.
        if (type.IsNotPublic || type.IsAbstract || type.IsInterface)
        {
            return false;
        }

        // Check all public constructors
        ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                               .Where(c => c.IsPublic)
                               .ToArray();

        if (constructors.Length == 0)
            return false;

        foreach (ConstructorInfo? constructor in constructors)
        {
            ParameterInfo[] parameters = constructor.GetParameters();

            // Parameterless constructor can be auto-generated
            if (parameters.Length == 0)
                return true;

            // Check if all parameters can be resolved via Dependency Injection

            if (_serviceProvider is null)
            {
                continue;
            }

            bool allParametersResolvable = true;
            foreach (ParameterInfo param in parameters)
            {
                try
                {
                    object? service = _serviceProvider.GetService(param.ParameterType);
                    if (service is null && !param.IsOptional)
                    {
                        allParametersResolvable = false;
                        break;
                    }
                }
                catch
                {
                    allParametersResolvable = false;
                    break;
                }
            }

            if (allParametersResolvable)
                return true;
        }

        return false;
    }

    private void AddPreview(PreviewReflection preview)
    {
        Type uiComponentType = preview.UIComponentType;

        string uiComponentName = uiComponentType.FullName ??
            throw new ArgumentException("Preview UIComponentType must have a FullName", nameof(preview));

        if (_uiComponentsByName.TryGetValue(uiComponentName, out UIComponentReflection? component))
        {
            component = (UIComponentReflection)component.WithAddedPreview(preview);
        }
        else
        {
            UIComponentKind kind = InferUIComponentKind(uiComponentType);
            component = new UIComponentReflection(uiComponentType, kind, null, [preview]);
        }

        _uiComponentsByName[uiComponentName] = component;
    }
}
