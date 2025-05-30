﻿namespace ExampleFramework;

public abstract class ExampleBase
{
    private readonly string? _displayName;

    public ExampleBase(string? displayName)
    {
        _displayName = displayName;
    }

    public virtual bool IsAutoGenerated => false;

    /// <summary>
    /// DisplayName is intended to be what's shown in UI to identify the example. It can contain spaces and
    /// isn't necessarily unique. It defaults to the example method/class name (with no namespace) but can
    /// be overridden by the developer.
    /// </summary>
    public string DisplayName => _displayName ?? NameUtilities.GetUnqualifiedName(Name);

    /// <summary>
    /// Name is intended to be used by code to uniquely identify the example. It's the example's
    /// full qualified method name.
    /// </summary>
    public abstract string Name { get; }
}
