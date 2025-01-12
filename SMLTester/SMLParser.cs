using System.Diagnostics;

namespace SMLTester
{
    public class SMLParser
    {
        public static SMLData Parse(List<byte> data)
        {
            while (data.Count > 0)
            {
                var package = ExtractPackage(data);
                if (package.Count == 0)
                {
                    return null;
                }
                var elements = ExtractNodes(package);

                if (elements.Count == 0)
                {
                    throw new Exception("Error while parsing SML package. No elements could be identified");
                }

                if (elements.Count < 2)
                {
                    throw new Exception("Error while parsing SML package. Expected 2 elements on root level, but found " + elements.Count);
                }

                if (elements[1] is not SMLList list)
                {
                    throw new Exception("Error while parsing SML package. Second element on root level is not a list");
                }

                var dataRootElement = elements[1] as SMLList;
                if (dataRootElement.elements.Count < 4)
                {
                    throw new Exception("Error while parsing SML package. Second list does not contain enough elements");
                }
                if (dataRootElement.elements[3] is not SMLList)
                {
                    throw new Exception("Error while parsing SML package. Fourth element on second level is not a list");
                }

                var dataLevel2Element = dataRootElement.elements[3] as SMLList;
                if (dataLevel2Element.elements.Count < 2)
                {
                    throw new Exception("Error while parsing SML package. Third list does not contain enough elements");
                }

                try
                {
                    var tarif1Element = ((SMLList)((SMLList)((SMLList)dataLevel2Element.elements[1]).elements[4]).elements[2]).elements[5] as SMLElement;
                    var tarif2Element = ((SMLList)((SMLList)((SMLList)dataLevel2Element.elements[1]).elements[4]).elements[3]).elements[5] as SMLElement;
                    var LeistungsElement = ((SMLList)((SMLList)((SMLList)dataLevel2Element.elements[1]).elements[4]).elements[4]).elements[5] as SMLElement;

                    var tarif1 = SMLElementToInteger(tarif1Element);
                    var tarif2 = SMLElementToInteger(tarif2Element);
                    var Leistung = SMLElementToInteger(LeistungsElement);

                    return new SMLData((decimal)tarif1 / 10000, (decimal)tarif2 / 10000, (decimal)Leistung);

                }
                catch (Exception ex)
                {
                    throw new Exception("Error while parsing SML package. " + ex.Message);
                }
            }
            return null; 
        }

        private static int SMLElementToInteger(SMLElement? byteData)
        {
            var tarif1Array = byteData.data.Skip(1).ToArray();
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(tarif1Array);
            }

            if (byteData.data[0] >> 4 == 5)
            {
                return BitConverter.ToInt16(tarif1Array, 0);
            }
            return BitConverter.ToInt32(tarif1Array, 0);
        }

        public static List<byte> ExtractPackage(List<byte> data)
        {
            var startSequence = new List<byte> { 0x1B, 0x1B, 0x1B, 0x1B, 0x01, 0x01, 0x01, 0x01 };
            var endSequencePrefix = new List<byte> { 0x1B, 0x1B, 0x1B, 0x1B, 0x1A };
            int startIndex = -1;
            int endIndex = -1;

            // Find the start sequence
            for (int i = 0; i <= data.Count - startSequence.Count; i++)
            {
                bool match = true;
                for (int j = 0; j < startSequence.Count; j++)
                {
                    if (data[i + j] != startSequence[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                {
                    startIndex = i + startSequence.Count;
                    break;
                }
            }

            if (startIndex == -1)
            {
                data.Clear(); // No start sequence found, clear the data
                return new List<byte>();
            }

            // Find the end sequence
            for (int i = startIndex; i <= data.Count - endSequencePrefix.Count - 3; i++)
            {
                bool match = true;
                for (int j = 0; j < endSequencePrefix.Count; j++)
                {
                    if (data[i + j] != endSequencePrefix[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                {
                    endIndex = i;
                    break;
                }
            }

            if (endIndex == -1)
            {
                return new List<byte>(); // No end sequence found
            }

            var package = data.GetRange(startIndex, endIndex - startIndex);
            data.RemoveRange(0, endIndex + endSequencePrefix.Count + 3);
            return package;
        }
        
        public static List<ISMLNode> ExtractNodes(List<byte> data)
        {
            int index = 0;
            return ExtractNodes(data, ref index, 99);
        }

        public static List<ISMLNode> ExtractNodes(List<byte> data, ref int index, int listitems)
        {
            var elements = new List<ISMLNode>();

            while (index < data.Count)
            {
                Debug.WriteLine($"data[{index}] -> {data[index]:X2}");
                int elementType = data[index] >> 4; // Extract the type from the start byte
                int elementLength = data[index] & 0x0F; // Extract the length from the start byte
                if (elementLength == 0) 
                    elementLength = 1; // If the element is 0x00, the element is 1 byte long

                if (elementType == 0x07)
                {
                    index++;
                    var element = new SMLList(elements: ExtractNodes(data, ref index, elementLength));
                    if (element.elements.Count != elementLength)
                    {
                         throw new ArgumentOutOfRangeException($"Number of elements in list ({element.elements.Count}) does not match the specified length for the list ({elementLength})");
                    }
                    elements.Add(element);
                }

                else
                {
                    if (index + elementLength > data.Count)
                    {
                        throw new IndexOutOfRangeException("Not enough data for the element");
                    }

                    var element = data.GetRange(index, elementLength);
                    elements.Add(new SMLElement(data: element));
                    index += elementLength; // Move to the next element
                }
                // Check if we have reached the expected number of elements for the current list
                if (elements.Count >= listitems)
                {
                    break;
                }
            }

            return elements;
        }
    }

    public interface ISMLNode { }

    public record SMLElement(List<Byte> data) : ISMLNode;

    public record SMLList(List<ISMLNode> elements) : ISMLNode;

    public record SMLData(decimal Tarif1, decimal Tarif2, decimal Power);
}
