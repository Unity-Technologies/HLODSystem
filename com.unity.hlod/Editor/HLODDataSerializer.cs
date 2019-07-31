using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Unity.HLODSystem
{
    public static class HLODDataSerializer
    {
        public static void Write(Stream stream, HLODData data)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, data);
        }

        public static HLODData Read(Stream stream)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            return formatter.Deserialize(stream) as HLODData;
        }
    }
}