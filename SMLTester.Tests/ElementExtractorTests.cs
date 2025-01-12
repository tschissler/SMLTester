using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMLTester.Tests
{
    [TestClass]
    public class ElementExtractorTests
    {
        [TestMethod]
        public void SingleByte_ReturnsSingleElement()
        {
            // Arrange
            var data = new List<byte> { 0x01 };
            var expectedElements = new List<SMLElement>
            {
                new SMLElement(data : new List < byte > { 0x01 })
            };

            // Act
            var result = SMLParser.ExtractNodes(data);

            // Assert
            Assert.AreEqual(expectedElements.Count, result.Count);
            for (int i = 0; i < expectedElements.Count; i++)
            {
                CollectionAssert.AreEqual(expectedElements[i].data, ((SMLElement)result[i]).data);
            }
        }

        [TestMethod]
        public void TwoBytes_ReturnsTwoElements()
        {
            // Arrange
            var data = new List<byte> { 0x01, 0x01 };

            var expectedElements = new List<SMLElement>
            {
                new SMLElement(data: new List<byte> { 0x01 }),
                new SMLElement(data: new List<byte> { 0x01 })
            };

            // Act
            var result = SMLParser.ExtractNodes(data);

            // Assert
            Assert.AreEqual(expectedElements.Count, result.Count);
            for (int i = 0; i < expectedElements.Count; i++)
            {
                CollectionAssert.AreEqual(expectedElements[i].data, ((SMLElement)result[i]).data);
            }
        }

        [TestMethod]
        public void Data62_52_01_ReturnsThreeElements()
        {
            // Arrange
            var data = new List<byte> { 0x63, 0x01, 0x01, 0x53, 0x01, 0x01, 0x01 };

            var expectedElements = new List<SMLElement>
            {
                new SMLElement(data: new List<byte> { 0x63, 0x01, 0x01 }),
                new SMLElement(data: new List<byte> { 0x53, 0x01, 0x01 }),
                new SMLElement(data: new List<byte> { 0x01 })
            };

            // Act
            var result = SMLParser.ExtractNodes(data);

            // Assert
            Assert.AreEqual(expectedElements.Count, result.Count);
            for (int i = 0; i < expectedElements.Count; i++)
            {
                CollectionAssert.AreEqual(expectedElements[i].data, ((SMLElement)result[i]).data);
            }
        }

        [TestMethod]
        public void StartbyteDefines5ByteElement_Returns5ByteElement()
        {
            // Arrange
            var data = new List<byte> { 0x05, 0x01, 0x02, 0x03, 0x04 };
            var expectedElements = new List<SMLElement>
            {
                new SMLElement(data: new List<byte> { 0x05, 0x01, 0x02, 0x03, 0x04 })
            };

            // Act
            var result = SMLParser.ExtractNodes(data);

            // Assert
            Assert.AreEqual(expectedElements.Count, result.Count);
            for (int i = 0; i < expectedElements.Count; i++)
            {
                CollectionAssert.AreEqual(expectedElements[i].data, ((SMLElement)result[i]).data);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(IndexOutOfRangeException))]
        public void StartbyteDefines5ByteElementDataHasONly4Bytes_Exception()
        {
            // Arrange
            var data = new List<byte> { 0x05, 0x01, 0x02, 0x03 };

            // Act
            var result = SMLParser.ExtractNodes(data);
        }

        [TestMethod]
        public void StartbyteDefinesList_ReturnsList()
        {
            // Arrange
            var data = new List<byte> { 0x72, 0x01, 0x01 };
            var expectedElements = new List<ISMLNode>
            {
                new SMLList(elements: new List<ISMLNode>()
                {
                    new SMLElement(data: new List<byte> { 0x01 }),
                    new SMLElement(data: new List<byte> { 0x01 })
                })
            };

            // Act
            var result = SMLParser.ExtractNodes(data);

            // Assert
            Assert.AreEqual(expectedElements.Count, expectedElements.Count);
            Assert.AreEqual(((SMLList)expectedElements[0]).elements.Count, ((SMLList)result[0]).elements.Count);
            for (int i = 0; i < ((SMLList)expectedElements[0]).elements.Count; i++)
            {
                CollectionAssert.AreEqual(((SMLElement)((SMLList)expectedElements[0]).elements[i]).data, ((SMLElement)((SMLList)result[0]).elements[i]).data);
            }
        }

        [TestMethod]
        public void ListWithSubList_ReturnsListwithSublist()
        {
            // Arrange
            var data = new List<byte> { 0x72, 0x01, 0x72, 0x63, 0x01, 0x01, 0x01 };
            var expectedElements = new List<ISMLNode>
            {
                new SMLList(elements: new List<ISMLNode>()
                {
                    new SMLElement(data: new List<byte> { 0x01 }),
                    new SMLList(elements: new List<ISMLNode>()
                    {
                        new SMLElement(data: new List<byte> { 0x63, 0x01, 0x01 }),
                        new SMLElement(data: new List<byte> { 0x01 })
                    })
                })
            };

            // Act
            var result = SMLParser.ExtractNodes(data);

            // Assert
            Assert.AreEqual(expectedElements.Count, expectedElements.Count);
            Assert.AreEqual(((SMLList)expectedElements[0]).elements.Count, ((SMLList)result[0]).elements.Count);
            Assert.AreEqual(((SMLList)((SMLList)expectedElements[0]).elements[1]).elements.Count, ((SMLList)((SMLList)result[0]).elements[1]).elements.Count);
            for (int i = 0; i < ((SMLList)((SMLList)expectedElements[0]).elements[1]).elements.Count; i++)
            {
                CollectionAssert.AreEqual(((SMLElement)((SMLList)((SMLList)expectedElements[0]).elements[1]).elements[i]).data, ((SMLElement)((SMLList)((SMLList)result[0]).elements[1]).elements[i]).data);
            }
        }

        [TestMethod]
        public void ListWithSubListAndFollowingElements_ReturnsListwithSublistAndElements()
        {
            // Arrange
            var data = new List<byte> { 0x73, 0x01, 0x72, 0x63, 0x01, 0x01, 0x01, 0x02, 0x03};
            var expectedElements = new List<ISMLNode>
            {
                new SMLList(elements: new List<ISMLNode>()
                {
                    new SMLElement(data: new List<byte> { 0x01 }),
                    new SMLList(elements: new List<ISMLNode>()
                    {
                        new SMLElement(data: new List<byte> { 0x63, 0x01, 0x01 }),
                        new SMLElement(data: new List<byte> { 0x01 })
                    }),
                    new SMLElement(data: new List<byte> { 0x01 })
                })
            };

            // Act
            var result = SMLParser.ExtractNodes(data);

            // Assert
            Assert.AreEqual(expectedElements.Count, expectedElements.Count);
            Assert.AreEqual(((SMLList)expectedElements[0]).elements.Count, ((SMLList)result[0]).elements.Count);
            Assert.AreEqual(((SMLList)((SMLList)expectedElements[0]).elements[1]).elements.Count, ((SMLList)((SMLList)result[0]).elements[1]).elements.Count);
            for (int i = 0; i < ((SMLList)((SMLList)expectedElements[0]).elements[1]).elements.Count; i++)
            {
                CollectionAssert.AreEqual(((SMLElement)((SMLList)((SMLList)expectedElements[0]).elements[1]).elements[i]).data, ((SMLElement)((SMLList)((SMLList)result[0]).elements[1]).elements[i]).data);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void NumberItemsInListIsSmallerThanListSize()
        {
            // Arrange
            var data = new List<byte> { 0x72, 0x01 };

            // Act
            var result = SMLParser.ExtractNodes(data);
        }
    }
}
