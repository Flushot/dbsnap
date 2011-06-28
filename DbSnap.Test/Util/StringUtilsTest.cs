using System;
using NUnit.Framework;
using DbSnap.Util;

namespace DbSnap.Test.Util
{
    [TestFixture]
    public class StringUtilsTest
    {
        [Test]
        public void test_NormalizeFilename()
        {
            Assert.AreEqual("testfile.dat", StringUtils.NormalizeFilename("testfile.dat", true));
            Assert.AreEqual("__some_file_.bin", StringUtils.NormalizeFilename("<\\some:file>.bin", true));
            Assert.AreEqual("dbo_table_sql", StringUtils.NormalizeFilename("[dbo].[table].sql", false));
            Assert.AreEqual("b_ah__", StringUtils.NormalizeFilename("b|ah\"?"));
            Assert.AreEqual("___", StringUtils.NormalizeFilename("/.*"));

            Assert.IsNull(StringUtils.NormalizeFilename(null));
            Assert.IsNull(StringUtils.NormalizeFilename(null, true));
        }

        [Test]
        public void test_IsInArray()
        {
            String[] haystack = { "apple", "banana", "orange", "PEACH" };

            Assert.IsTrue(StringUtils.IsInArray("apple", haystack, false));
            Assert.IsFalse(StringUtils.IsInArray("APPLE", haystack, false));
            Assert.IsFalse(StringUtils.IsInArray("truck", haystack, false));
            Assert.IsTrue(StringUtils.IsInArray("peach", haystack, true));
            Assert.IsTrue(StringUtils.IsInArray("PeAcH", haystack, true));
            Assert.IsFalse(StringUtils.IsInArray("car", haystack, true));

            Assert.IsFalse(StringUtils.IsInArray(null, haystack, true));
            Assert.IsFalse(StringUtils.IsInArray(null, haystack, false));
            Assert.IsFalse(StringUtils.IsInArray("apple", null, false));
            Assert.IsFalse(StringUtils.IsInArray("apple", null, true));
        }
    }
}
