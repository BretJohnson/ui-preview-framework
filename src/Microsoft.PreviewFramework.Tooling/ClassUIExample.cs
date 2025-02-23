﻿namespace Microsoft.PreviewFramework.Tooling;

public class ClassUIExample : ToolingUIExample
{
    bool isAutoGenerated = false;

    public ClassUIExample(string classFullName, string? displayName) : base(classFullName, displayName)
    {
    }

    public ClassUIExample(string classFullName, bool isAutoGenerated) : base(classFullName, null)
    {
        this.isAutoGenerated = isAutoGenerated;
    }

    public override bool IsAutoGenerated => this.isAutoGenerated;
}
