using Xunit;
using Imageflow.Fluent;
namespace Imageflow.Test
{
    public class TestSrgbColor
    {
        
        [Fact]
        public void TestFromHex()
        {
            foreach (var color in new string[] {"1234", "11223344"})
            {
                var parsed = SrgbColor.FromHex(color);
                Assert.Equal("11", $"{parsed.R:x2}");
                Assert.Equal("22", $"{parsed.G:x2}");
                Assert.Equal("33", $"{parsed.B:x2}");
                Assert.Equal("44", $"{parsed.A:x2}");
            }

            foreach (var color in new string[] {"123", "112233"})
            {
                var parsed = SrgbColor.FromHex(color);
                Assert.Equal("11", $"{parsed.R:x2}");
                Assert.Equal("22", $"{parsed.G:x2}");
                Assert.Equal("33", $"{parsed.B:x2}");
                Assert.Equal("ff", $"{parsed.A:x2}");
            }
        }
        
        [Fact]
        public void TestRoundTrip()
        {
            Assert.Equal("11223344", SrgbColor.FromHex("11223344").ToHexUnprefixed());
            Assert.Equal("11223344", SrgbColor.FromHex("1234").ToHexUnprefixed());
            Assert.Equal("112233", SrgbColor.FromHex("112233").ToHexUnprefixed());
            Assert.Equal("112233", SrgbColor.FromHex("123").ToHexUnprefixed());
        }
    }
}