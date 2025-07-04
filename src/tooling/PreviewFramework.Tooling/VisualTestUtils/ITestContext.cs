namespace PreviewFramework.Tooling.VisualTestUtils
{
    /// <summary>
    /// This interface can be implemented by the client and is used to associate
    /// extra information like file attachments with failed visual tests, if supported
    /// by the client's testing framework.
    ///
    /// A typical client implemention would do this, depending on client test framework:
    /// NUnit: Call NUnit TestContext.AddTestAttachment (https://docs.nunit.org/articles/nunit/writing-tests/TestContext.html)
    /// XUnit: Not currently supported; see https://github.com/xunit/xunit/issues/2457
    /// MSTest: Call TestContext.AddResultFile (https://learn.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.testtools.unittesting.testcontext.addresultfile?view=visualstudiosdk-2022)
    /// </summary>
    public interface ITestContext
    {
        /// <summary>
        /// Add a test attachment, associating the specified file with the test.
        /// </summary>
        /// <param name="filePath">path to file to attach to test</param>
        void AddTestAttachment(string filePath);
    }
}
