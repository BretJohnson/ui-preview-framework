﻿namespace Microsoft.PreviewFramework.App;

public class ClassUIExample : AppUIExample
{
    bool isAutoGenerated = false;

    public ClassUIExample(UIExampleAttribute uiExampleAttribute, Type type) : base(uiExampleAttribute)
    {
        this.Type = type;
    }

    public ClassUIExample(Type type, bool isAutoGenerated) : base(type)
    {
        this.Type = type;
        this.isAutoGenerated = isAutoGenerated;
    }

    public Type Type { get; }

    public override object Create()
    {
        return Activator.CreateInstance(this.Type);
    }

    public override Type? DefaultUIComponentType => null;

    public override string Name => this.Type.FullName;

    public override bool IsAutoGenerated => this.isAutoGenerated;
}
