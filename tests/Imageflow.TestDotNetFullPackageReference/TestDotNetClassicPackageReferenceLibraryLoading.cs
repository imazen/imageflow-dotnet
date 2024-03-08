namespace Imageflow.TestDotNetFullPackageReference;

[Microsoft.VisualStudio.TestTools.UnitTesting.TestClass]
public class TestDotNetClassicPackageReferenceLibraryLoading
{
[Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
public void TestAccessAbi()
{
    using (var j = new Bindings.JobContext()) { }
    }
}
