﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace PreviewFramework;

public abstract class UIComponentBase<TPreview> where TPreview : PreviewBase
{
    private readonly string? _displayName;
    private readonly List<TPreview> _previews = [];

    public UIComponentBase(UIComponentKind kind, string? displayName)
    {
        _displayName = displayName;
        Kind = kind;
    }

    /// <summary>
    /// Name is intended to be what's used by the code to identify the component. It's the component's
    /// full qualified type name and is unique.
    /// </summary>
    public abstract string Name { get; }

    public UIComponentCategory? Category { get; private set; }

    public UIComponentKind Kind { get; }

    /// <summary>
    /// DisplayName is intended to be what's shown in the UI to identify the component. It can contain spaces and
    /// isn't necessarily unique. It defaults to the class name (with no namespace qualifier) but can be
    /// overridden by the developer.
    /// </summary>
    public string DisplayName => _displayName ?? NameUtilities.GetUnqualifiedName(Name);

    public bool HasPreview => _previews.Count >= 0;

    public bool HasNoPreviews => _previews.Count == 0;

    public bool HasSinglePreview => _previews.Count == 1;

    public bool HasMultiplePreviews => _previews.Count > 1;

    public void InitCategory(UIComponentCategory category)
    {
        if (Category is not null && string.Compare(this.Category.Name, category.Name, StringComparison.OrdinalIgnoreCase) != 0)
        {
            throw new InvalidOperationException($"Component '{Name}' can't be set to category '{category.Name}' since it already has category '{this.Category.Name}' set");
        }
        Category = category;
    }

    public IReadOnlyList<TPreview> Previews => _previews;

    public TPreview? GetPreview(string name)
    {
        foreach (TPreview preview in _previews)
        {
            if (preview.Name.Equals(name, StringComparison.Ordinal))
            {
                return preview;
            }
        }

        return null;
    }

    public TPreview DefaultPreview
    {
        get
        {
            if (_previews.Count == 0)
            {
                throw new InvalidOperationException($"Component '{Name}' has no previews");
            }

            // Currently, the default preview is always the first one, though we may allow
            // it to be set explicitly in the future
            return _previews[0];
        }
    }

    public void AddPreview(TPreview preview)
    {
        _previews.Add(preview);

        // If there's a user defined preview, remove any auto-generated previews
        if (!preview.IsAutoGenerated)
        {
            RemoveAutoGeneratedPreviews();
        }
    }

    public bool IsAutoGenerated => _previews.All(e => e.IsAutoGenerated);

    private void RemoveAutoGeneratedPreviews()
    {
        _previews.RemoveAll(e => e.IsAutoGenerated);
    }
}
