using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Imageflow.TestDotNetFull
{
    [TestClass]
    public class TestDotNetClassicLibraryLoading
    {
        [TestMethod]
        public void TestAccessAbi()
        {
            using (var j = new Imageflow.Bindings.JobContext()) { }
        }
    }
}
