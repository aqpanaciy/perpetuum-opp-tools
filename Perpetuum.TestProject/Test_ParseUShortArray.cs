// Ignore Spelling: Perpetuum

using Perpetuum.ClassLibrary;

namespace Perpetuum.TestProject
{
    [TestClass]
    public sealed class Test_ParseUShortArray
    {
        [TestMethod]
        [DataRow("1,5,10,100", new ushort[] { 1, 5, 10, 100 })]
        [DataRow("0,10,65535", new ushort[] { 0, 10, 65535 })]
        [DataRow("", new ushort[] { })]
        public void TestMethod_ValidString(string values, ushort[] result)
        {
            var actual = ClassUtility.ParseUShortArray(values);
            CollectionAssert.AreEqual(result, actual);
        }

        [TestMethod]
        [DataRow("1,2,2,3")]
        [DataRow("1,5,3")]
        [DataRow("1,65536")]
        [DataRow("1,-2,3")]
        [DataRow("1,,3")]
        public void TestMethod_InvalidString(string values)
        {
            try
            {
                ClassUtility.ParseUShortArray(values);
                throw new Exception("Improper behavior, an exception should be thrown.");
            }
            catch (FormatException)
            {
                // Everything is fine; this is expected behavior.
            }
            catch
            {
                throw new Exception("Incorrect behavior, unknown exception.");
            }
        }

        [TestMethod]
        [DataRow(null)]
        public void TestMethod_EmptyString(string values)
        {
            try
            {
                ClassUtility.ParseUShortArray(values);
                throw new Exception("Improper behavior, an exception should be thrown.");
            }
            catch (ArgumentNullException)
            {
                // Everything is fine; this is expected behavior.
            }
            catch
            {
                throw new Exception("Incorrect behavior, unknown exception.");
            }
        }
    }
}
