using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Imageflow.TestDotNetFullPackageReference
{
    [TestClass]
    public class TestDotNetClassicPackageReferenceLibraryLoading
    {
        [TestMethod]
        public void TestAccessAbi()
        {
            using (var j = new Bindings.JobContext()) { }
        }
    }
}
