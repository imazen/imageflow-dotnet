using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Imageflow.TestDotNetFullPackageReference
{
    [TestClass]
    public class TestDotNetClassicPackageReferenceLibraryLoading
    {
        [TestMethod]
        public void TestAccessAbi()
        {
            using (var j = new Imageflow.Bindings.JobContext()) { }
        }
    }
}
