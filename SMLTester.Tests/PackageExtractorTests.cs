using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace SMLTester.Tests
{
    [TestClass]
    public class PackageExtractorTests
    {
        [TestMethod]
        public void ExtractPackage_ValidData_ReturnsExpectedPackage()
        {
            // Arrange
            var data = new List<byte>
            {
                0x1B, 0x1B, 0x1B, 0x1B,
                0x01, 0x01, 0x01, 0x01,
                0xAA, 0xBB, 0xCC, 0xDD,
                0x1B, 0x1B, 0x1B, 0x1B,
                0x1A, 0x01, 0x1B, 0x72
            };
            var expectedPackage = new List<byte>
            {
                0xAA, 0xBB, 0xCC, 0xDD
            };

            // Act
            var result = SMLParser.ExtractPackage(data);

            // Assert
            CollectionAssert.AreEqual(expectedPackage, result);
        }

        [TestMethod]
        public void ExtractPackage_DataWithLeadingBytes_IgnoresLeadingBytes()
        {
            // Arrange
            var data = new List<byte>
            {
                0x00, 0x00, 0x00, 0x00, // Leading bytes to ignore
                0x1B, 0x1B, 0x1B, 0x1B,
                0x01, 0x01, 0x01, 0x01,
                0xAA, 0xBB, 0xCC, 0xDD,
                0x1B, 0x1B, 0x1B, 0x1B,
                0x1A, 0x01, 0x1B, 0x72
            };
            var expectedPackage = new List<byte>
            {
                0xAA, 0xBB, 0xCC, 0xDD
            };

            // Act
            var result = SMLParser.ExtractPackage(data);

            // Assert
            CollectionAssert.AreEqual(expectedPackage, result);
        }

        [TestMethod]
        public void ExtractPackage_SinglePackage_RemovedFromData()
        {
            // Arrange
            var data = new List<byte>
            {
                0x1B, 0x1B, 0x1B, 0x1B,
                0x01, 0x01, 0x01, 0x01,
                0xAA, 0xBB, 0xCC, 0xDD,
                0x1B, 0x1B, 0x1B, 0x1B,
                0x1A, 0x01, 0x1B, 0x72
            };
            var expectedPackage = new List<byte>
            {
                0xAA, 0xBB, 0xCC, 0xDD
            };

            // Act
            var result = SMLParser.ExtractPackage(data);
            var resultAfterRemoval = SMLParser.ExtractPackage(data);

            // Assert
            CollectionAssert.AreEqual(expectedPackage, result);
            Assert.AreEqual(0, resultAfterRemoval.Count);
        }

        [TestMethod]
        public void ExtractPackage_TwoPackages_ExtractsInOrder()
        {
            // Arrange
            var data = new List<byte>
            {
                // First package
                0x1B, 0x1B, 0x1B, 0x1B,
                0x01, 0x01, 0x01, 0x01,
                0xAA, 0xBB, 0xCC, 0xDD,
                0x1B, 0x1B, 0x1B, 0x1B,
                0x1A, 0x01, 0x1B, 0x72,
                // Second package
                0x1B, 0x1B, 0x1B, 0x1B,
                0x01, 0x01, 0x01, 0x01,
                0xEE, 0xFF, 0x11, 0x22,
                0x1B, 0x1B, 0x1B, 0x1B,
                0x1A, 0x01, 0x1B, 0x73
            };
            var expectedFirstPackage = new List<byte>
            {
                0xAA, 0xBB, 0xCC, 0xDD
            };
            var expectedSecondPackage = new List<byte>
            {
                0xEE, 0xFF, 0x11, 0x22
            };

            // Act
            var firstResult = SMLParser.ExtractPackage(data);
            var secondResult = SMLParser.ExtractPackage(data);

            // Assert
            CollectionAssert.AreEqual(expectedFirstPackage, firstResult);
            CollectionAssert.AreEqual(expectedSecondPackage, secondResult);
        }

        [TestMethod]
        public void ExtractPackage_DataWithLeadingBytes_RemovesLeadingBytes()
        {
            // Arrange
            var data = new List<byte>
            {
                0x00, 0x00, 0x00, 0x00, // Leading bytes to remove
                0x1B, 0x1B, 0x1B, 0x1B,
                0x01, 0x01, 0x01, 0x01,
                0xAA, 0xBB, 0xCC, 0xDD,
                0x1B, 0x1B, 0x1B, 0x1B,
                0x1A, 0x01, 0x1B, 0x72,
                0xFF, 0xFF, 0xFF // Trailing bytes to keep
            };
            var expectedPackage = new List<byte>
            {
                0xAA, 0xBB, 0xCC, 0xDD
            };
            var expectedRemainingData = new List<byte>
            {
                0xFF, 0xFF, 0xFF // Trailing bytes to keep
            };

            // Act
            var result = SMLParser.ExtractPackage(data);

            // Assert
            CollectionAssert.AreEqual(expectedPackage, result);
            CollectionAssert.AreEqual(expectedRemainingData, data);
        }

        [TestMethod]
        public void ExtractPackage_IncompletePackage_KeepsData()
        {
            // Arrange
            var data = new List<byte>
            {
                0x1B, 0x1B, 0x1B, 0x1B,
                0x01, 0x01, 0x01, 0x01,
                0xAA, 0xBB, 0xCC, 0xDD,
                0x1B, 0x1B, 0x1B, 0x1B,
                0x1A // Incomplete end sequence
            };
            var expectedRemainingData = new List<byte>
            {
                0x1B, 0x1B, 0x1B, 0x1B,
                0x01, 0x01, 0x01, 0x01,
                0xAA, 0xBB, 0xCC, 0xDD,
                0x1B, 0x1B, 0x1B, 0x1B,
                0x1A // Incomplete end sequence
            };

            // Act
            var result = SMLParser.ExtractPackage(data);

            // Assert
            Assert.AreEqual(0, result.Count); // No package should be returned
            CollectionAssert.AreEqual(expectedRemainingData, data); // Data should remain unchanged
        }

        [TestMethod]
        public void ExtractPackage_IncompletePackageWithoutFullStartsequence_KeepsData()
        {
            // Arrange
            var data = new List<byte>
             {
                 0xAA, 0xBB, 0xCC, 0xDD,
                 0x1B, 0x1B // Incomplete start sequence
             };
                    var expectedRemainingData = new List<byte>
             {
                 0xAA, 0xBB, 0xCC, 0xDD,
                 0x1B, 0x1B // Incomplete start sequence
             };

            // Act
            var result = SMLParser.ExtractPackage(data);

            // Assert
            Assert.AreEqual(0, result.Count); // No package should be returned
            CollectionAssert.AreEqual(expectedRemainingData, data); // Data should remain unchanged
        }
    }
}
