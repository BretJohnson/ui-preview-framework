﻿namespace Microsoft.PreviewFramework.App;

public class ExampleNotFoundException : Exception
{
    public ExampleNotFoundException(string message) : base(message)
    {
    }
}
