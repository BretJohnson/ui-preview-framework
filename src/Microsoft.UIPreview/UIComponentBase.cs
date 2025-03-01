﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.UIPreview;

public abstract class UIComponentBase<TPreview> where TPreview : PreviewBase
{
    private UIComponentCategory? category;
    private readonly string? displayName;
    private readonly List<TPreview> previews = new();

    public UIComponentBase(string? displayName)
    {
        this.displayName = displayName;
    }

    /// <summary>
    /// DisplayName is intended to be what's shown in the UI to identify the component. It can contain spaces and
    /// isn't necessarily unique. It defaults to the class name (with no namespace qualifier) but can be
    /// overridden by the developer.
    /// </summary>
    public string DisplayName => this.displayName ?? NameUtilities.GetUnqualifiedName(this.Name);

    /// <summary>
    /// Name is intended to be what's used by the code to identify the component. It's the component's
    /// full qualified type name and is unique.
    /// </summary>
    public abstract string Name { get; }

    public UIComponentCategory? Category => this.category;

    public void InitCategory(UIComponentCategory category)
    {
        if (this.category != null && string.Compare(this.category.Name, category.Name, StringComparison.OrdinalIgnoreCase) != 0)
        {
            throw new InvalidOperationException($"Component '{this.Name}' can't be set to category '{category.Name}' since it already has category '{this.category.Name}' set");
        }
        this.category = category;
    }

    public IReadOnlyList<TPreview> Previews => this.previews;

    public TPreview? GetPreview(string name)
    {
        foreach (TPreview preview in this.previews)
        {
            if (preview.Name.Equals(name, StringComparison.Ordinal))
            {
                return preview;
            }
        }

        return null;
    }

    public void AddPreview(TPreview preview)
    {
        this.previews.Add(preview);

        // If there's a user defined preview, remove any auto-generated previews
        if (!preview.IsAutoGenerated)
        {
            this.RemoveAutoGeneratedPreviews();
        }
    }

    public bool IsAutoGenerated => this.previews.All(e => e.IsAutoGenerated);


    private void RemoveAutoGeneratedPreviews()
    {
        this.previews.RemoveAll(e => e.IsAutoGenerated);
    }
}
