using System.Diagnostics;
using System.Linq;
using System.Text;

namespace SMLTester
{
    public class SMLParser
    {
        // 256-entry CRC table (polynomial 0x1021, bit-reflected to 0x8408)
        private static readonly ushort[] CrcTable = new ushort[256]
         {
            0x0000, 0x1189, 0x2312, 0x329B, 0x4624, 0x57AD, 0x6536, 0x74BF, 0x8C48,
            0x9DC1, 0xAF5A, 0xBED3, 0xCA6C, 0xDBE5, 0xE97E, 0xF8F7, 0x1081, 0x0108,
            0x3393, 0x221A, 0x56A5, 0x472C, 0x75B7, 0x643E, 0x9CC9, 0x8D40, 0xBFDB,
            0xAE52, 0xDAED, 0xCB64, 0xF9FF, 0xE876, 0x2102, 0x308B, 0x0210, 0x1399,
            0x6726, 0x76AF, 0x4434, 0x55BD, 0xAD4A, 0xBCC3, 0x8E58, 0x9FD1, 0xEB6E,
            0xFAE7, 0xC87C, 0xD9F5, 0x3183, 0x200A, 0x1291, 0x0318, 0x77A7, 0x662E,
            0x54B5, 0x453C, 0xBDCB, 0xAC42, 0x9ED9, 0x8F50, 0xFBEF, 0xEA66, 0xD8FD,
            0xC974, 0x4204, 0x538D, 0x6116, 0x709F, 0x0420, 0x15A9, 0x2732, 0x36BB,
            0xCE4C, 0xDFC5, 0xED5E, 0xFCD7, 0x8868, 0x99E1, 0xAB7A, 0xBAF3, 0x5285,
            0x430C, 0x7197, 0x601E, 0x14A1, 0x0528, 0x37B3, 0x263A, 0xDECD, 0xCF44,
            0xFDDF, 0xEC56, 0x98E9, 0x8960, 0xBBFB, 0xAA72, 0x6306, 0x728F, 0x4014,
            0x519D, 0x2522, 0x34AB, 0x0630, 0x17B9, 0xEF4E, 0xFEC7, 0xCC5C, 0xDDD5,
            0xA96A, 0xB8E3, 0x8A78, 0x9BF1, 0x7387, 0x620E, 0x5095, 0x411C, 0x35A3,
            0x242A, 0x16B1, 0x0738, 0xFFCF, 0xEE46, 0xDCDD, 0xCD54, 0xB9EB, 0xA862,
            0x9AF9, 0x8B70, 0x8408, 0x9581, 0xA71A, 0xB693, 0xC22C, 0xD3A5, 0xE13E,
            0xF0B7, 0x0840, 0x19C9, 0x2B52, 0x3ADB, 0x4E64, 0x5FED, 0x6D76, 0x7CFF,
            0x9489, 0x8500, 0xB79B, 0xA612, 0xD2AD, 0xC324, 0xF1BF, 0xE036, 0x18C1,
            0x0948, 0x3BD3, 0x2A5A, 0x5EE5, 0x4F6C, 0x7DF7, 0x6C7E, 0xA50A, 0xB483,
            0x8618, 0x9791, 0xE32E, 0xF2A7, 0xC03C, 0xD1B5, 0x2942, 0x38CB, 0x0A50,
            0x1BD9, 0x6F66, 0x7EEF, 0x4C74, 0x5DFD, 0xB58B, 0xA402, 0x9699, 0x8710,
            0xF3AF, 0xE226, 0xD0BD, 0xC134, 0x39C3, 0x284A, 0x1AD1, 0x0B58, 0x7FE7,
            0x6E6E, 0x5CF5, 0x4D7C, 0xC60C, 0xD785, 0xE51E, 0xF497, 0x8028, 0x91A1,
            0xA33A, 0xB2B3, 0x4A44, 0x5BCD, 0x6956, 0x78DF, 0x0C60, 0x1DE9, 0x2F72,
            0x3EFB, 0xD68D, 0xC704, 0xF59F, 0xE416, 0x90A9, 0x8120, 0xB3BB, 0xA232,
            0x5AC5, 0x4B4C, 0x79D7, 0x685E, 0x1CE1, 0x0D68, 0x3FF3, 0x2E7A, 0xE70E,
            0xF687, 0xC41C, 0xD595, 0xA12A, 0xB0A3, 0x8238, 0x93B1, 0x6B46, 0x7ACF,
            0x4854, 0x59DD, 0x2D62, 0x3CEB, 0x0E70, 0x1FF9, 0xF78F, 0xE606, 0xD49D,
            0xC514, 0xB1AB, 0xA022, 0x92B9, 0x8330, 0x7BC7, 0x6A4E, 0x58D5, 0x495C,
            0x3DE3, 0x2C6A, 0x1EF1, 0x0F78
         };

        // <summary>
        /// Compute CRC-16/IBM-SDLC over the given data.
        /// </summary>
        public static ushort Compute(byte[] data, int offset, int length)
        {
            const ushort Init = 0xFFFF;   // initial remainder :contentReference[oaicite:6]{index=6}
            const ushort XorOut = 0xFFFF; // final XOR :contentReference[oaicite:7]{index=7}

            ushort crc = Init;
            for (int i = offset; i < offset + length; i++)
            {
                byte b = data[i];
                // reflect input byte
                crc = (ushort)((crc >> 8) ^ CrcTable[(crc ^ b) & 0xFF]);
            }
            // reflect output CRC
            return (ushort)(crc ^ XorOut);
        }

        /// <summary>
        /// Verifies that the last two bytes of 'buffer' match the CRC.
        /// </summary>
        public static bool Verify(byte[] buffer)
        {
            int n = buffer.Length;
            // everything except trailing CRC bytes
            ushort computed = Compute(buffer, 0, n - 2);
            // received CRC is little-endian (LSB, then MSB)
            ushort received = (ushort)((buffer[n - 1] << 8) | buffer[n - 2]);
            return computed == received;
        }

        public static SMLData? Parse(List<byte> data)
        {
            while (data.Count > 0)
            {
                var package = ExtractPackage(data);
                if (package.Count == 0)
                    return null;
                var smlMessages = ExtractNodes(package);

                if (smlMessages.Count == 0)
                    throw new Exception("Error while parsing SML package. No SML messages could be identified");
                if (smlMessages.Count < 2)
                    throw new Exception("Error while parsing SML package. Expected at least 2 SML messages, but found " + smlMessages.Count);
                if (smlMessages[1] is not SMLList list)
                    throw new Exception("Error while parsing SML package. Second message is not a list");

                // The whole data package delivers multiple SML messages but we are only interested in the second one
                var dataSMLMessage = smlMessages[1] as SMLList;
                if (dataSMLMessage!.elements.Count < 4)
                    throw new Exception("Error while parsing SML package. SML message with data does not contain enough elements");
                if (dataSMLMessage.elements[3] is not SMLList)
                    throw new Exception("Error while parsing SML package. Fourth element SML data message is not a list");

                // That SML message should contain exactly one list element (72)
                var dataList = dataSMLMessage.elements.Where(i => i is SMLList);
                if (dataList.Count() != 1)
                    throw new Exception($"Error while parsing SML package. Expected exactly one list in the SML data message but found {dataList.Count()}");
                // In that list element we again select all sub-elements that are lists and continue by using the first list we find (77)
                var subDataList = ((SMLList)((SMLList)dataList.ElementAt(0)).elements.Where(i => i is SMLList).ElementAt(0)).elements;
                if (subDataList.Count() < 2)
                    throw new Exception($"Error while parsing SML package. Expected at least two lists in subDataLIst, but found {subDataList.Count()}");
                // In that list we again search for all list elements and take the second one (77).
                // This is now the list that finally contains the data objects.
                // The data objects themselves are again list elements containing an identifier at the first element and the value in the sixth element
                var valuesList = ((SMLList)subDataList.Where(i => i is SMLList).ElementAt(1)).elements.ToList().Cast<SMLList>();
                if (valuesList.Count() < 2)
                {
                    throw new Exception("Error while parsing SML package. Values list does not contain enough elements");
                }

                try
                {
                    var result = new SMLData();

                    // So far I found 2 different identifiers for the manufacturer ID
                    var valueElement = valuesList.FirstOrDefault(i => i.elements[0] is SMLElement && ((SMLElement)i.elements[0]).data.SequenceEqual(new List<Byte> { 0x07, 0x81, 0x81, 0xC7, 0x82, 0x03, 0xFF }));
                    if (valueElement is not null) result.ManufacturerId = SMLElementToString(((SMLElement)valueElement.elements[5]));
                    valueElement = valuesList.FirstOrDefault(i => i.elements[0] is SMLElement && ((SMLElement)i.elements[0]).data.SequenceEqual(new List<Byte> { 0x07, 0x01, 0x00, 0x60, 0x32, 0x01, 0x01 }));
                    if (valueElement is not null) result.ManufacturerId = SMLElementToString(((SMLElement)valueElement.elements[5]));

                    // So far I found 2 different identifiers for the manufacturer ID
                    valueElement = valuesList.FirstOrDefault(i => i.elements[0] is SMLElement && ((SMLElement)i.elements[0]).data.SequenceEqual(new List<Byte> { 0x07, 0x01, 0x00, 0x00, 0x00, 0x09, 0xFF }));
                    if (valueElement is not null) result.DeviceId = SMLElementToString(((SMLElement)valueElement.elements[5]));
                    valueElement = valuesList.FirstOrDefault(i => i.elements[0] is SMLElement && ((SMLElement)i.elements[0]).data.SequenceEqual(new List<Byte> { 0x07, 0x01, 0x00, 0x60, 0x01, 0x00, 0xFF }));
                    if (valueElement is not null) result.DeviceId = SMLElementToString(((SMLElement)valueElement.elements[5]));

                    valueElement = valuesList.FirstOrDefault(i => i.elements[0] is SMLElement && ((SMLElement)i.elements[0]).data.SequenceEqual(new List<Byte> { 0x07, 0x01, 0x00, 0x01, 0x08, 0x00, 0xFF }));
                    if (valueElement is not null) result.ConsumptionEnergyTotal = SMLElementToInteger(((SMLElement)valueElement.elements[5]))/ 10000m;

                    valueElement = valuesList.FirstOrDefault(i => i.elements[0] is SMLElement && ((SMLElement)i.elements[0]).data.SequenceEqual(new List<Byte> { 0x07, 0x01, 0x00, 0x01, 0x08, 0x01, 0xFF }));
                    if (valueElement is not null) result.ConsumptionEnergy1 = SMLElementToInteger(((SMLElement)valueElement.elements[5]))/ 10000m;

                    valueElement = valuesList.FirstOrDefault(i => i.elements[0] is SMLElement && ((SMLElement)i.elements[0]).data.SequenceEqual(new List<Byte> { 0x07, 0x01, 0x00, 0x01, 0x08, 0x02, 0xFF }));
                    if (valueElement is not null) result.ConsumptionEnergy2 = SMLElementToInteger(((SMLElement)valueElement.elements[5]))/10000m;

                    valueElement = valuesList.FirstOrDefault(i => i.elements[0] is SMLElement && ((SMLElement)i.elements[0]).data.SequenceEqual(new List<Byte> { 0x07, 0x01, 0x00, 0x02, 0x08, 0x00, 0xFF }));
                    if (valueElement is not null) result.FeedEnergyTotal = SMLElementToInteger(((SMLElement)valueElement.elements[5]))/10000m;

                    valueElement = valuesList.FirstOrDefault(i => i.elements[0] is SMLElement && ((SMLElement)i.elements[0]).data.SequenceEqual(new List<Byte> { 0x07, 0x01, 0x00, 0x02, 0x08, 0x01, 0xFF }));
                    if (valueElement is not null) result.FeedEnergy1 = SMLElementToInteger(((SMLElement)valueElement.elements[5]))/10000m;

                    valueElement = valuesList.FirstOrDefault(i => i.elements[0] is SMLElement && ((SMLElement)i.elements[0]).data.SequenceEqual(new List<Byte> { 0x07, 0x01, 0x00, 0x02, 0x08, 0x02, 0xFF }));
                    if (valueElement is not null) result.FeedEnergy2 = SMLElementToInteger(((SMLElement)valueElement.elements[5]))/10000m;

                    valueElement = valuesList.FirstOrDefault(i => i.elements[0] is SMLElement && ((SMLElement)i.elements[0]).data.SequenceEqual(new List<Byte> { 0x07, 0x01, 0x00, 0x10, 0x07, 0x00, 0xFF }));
                    if (valueElement is not null) result.EffectivePower = SMLElementToInteger(((SMLElement)valueElement.elements[5]))/1m;

                    return result;

                    //var tarif1Element = ((SMLList)((SMLList)((SMLList)dataSMLMessage.elements[1]).elements[4]).elements[2]).elements[5] as SMLElement;
                    //var tarif2Element = ((SMLList)((SMLList)((SMLList)dataSMLMessage.elements[1]).elements[4]).elements[3]).elements[5] as SMLElement;
                    //var LeistungsElement = ((SMLList)((SMLList)((SMLList)dataSMLMessage.elements[1]).elements[4]).elements[4]).elements[5] as SMLElement;

                    //var tarif1 = SMLElementToInteger(tarif1Element);
                    //var tarif2 = SMLElementToInteger(tarif2Element);
                    //var Leistung = SMLElementToInteger(LeistungsElement);

                    //return new SMLData((decimal)tarif1 / 10000, (decimal)tarif2 / 10000, (decimal)Leistung);

                }
                catch (Exception ex)
                {
                    throw new Exception("Error while parsing SML package. " + ex.Message);
                }
            }
            return null; 
        }

        private static int SMLElementToInteger(SMLElement byteData)
        {
            var integerArray = byteData.data.Skip(1).ToArray();
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(integerArray);
            }

            int type = byteData.data[0] >> 4;
            int length = byteData.data[0] & 0x0F;

            if (type == 5) // Signed Integers
            {
                switch (length)
                {
                    case 2:
                        return (sbyte)integerArray[0];
                    case 3:
                    case 4:
                        return BitConverter.ToInt16(integerArray, 0);
                    case 5:
                    case 6:
                        return BitConverter.ToInt32(integerArray, 0);
                    default:
                        throw new Exception($"Error while parsing integer, found length {length} but only 2, 3 and 5 are allowed");
                }
            }
            else if (type == 6) // Unsigned Integers
            {
                switch (length)
                {
                    case 2:
                        return integerArray[0];
                    case 3:
                    case 4:
                        return BitConverter.ToUInt16(integerArray, 0);
                    case 5:
                    case 6:
                        return (int)BitConverter.ToUInt32(integerArray, 0);
                    default:
                        throw new Exception($"Error while parsing unsigned integer, found length {length} but only 2, 3 and 5 are allowed");
                }
            }
            throw new Exception($"Error while parsing integer, found type {type} but only 5 (signed) and 6 (unsigned) are allowed");

        }

        public static string? SMLElementToString(SMLElement element)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in element.data)
            {
                sb.Append(b.ToString("X2"));
            }
            return sb.ToString();
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

                if (elementType == 0x08)
                {
                    elementLength = (elementLength << 4) + data[index + 1];
                }
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

    //public record SMLData(decimal Tarif1, decimal Tarif2, decimal Power);

    public class SMLData
    {
        // 81 81 C7 82 03 FF
        public string? ManufacturerId { get; set; }
        // 01 00 00 00 09 FF
        public string? DeviceId { get; set; }
        // 01 00 01 08 00 FF
        public decimal? ConsumptionEnergyTotal { get; set; }
        // 01 00 01 08 01 FF
        public decimal? ConsumptionEnergy1 { get; set; }
        // 01 00 01 08 02 FF
        public decimal? ConsumptionEnergy2 { get; set; }
        // 01 00 02 08 00 FF
        public decimal? FeedEnergyTotal { get; set; }
        // 01 00 02 08 01 FF
        public decimal? FeedEnergy1 { get; set; }
        // 01 00 02 08 02 FF
        public decimal? FeedEnergy2 { get; set; }
        // 01 00 10 07 00 FF
        public decimal? EffectivePower { get; set; }
    }

}
