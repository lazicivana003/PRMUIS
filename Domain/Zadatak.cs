using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Domain
{
    [Serializable]
    public class Zadatak
    {
        public int Id { get; set; }
        public int KlijentId { get; set; }
        public int VoziloId { get; set; }
        public string Status { get; set; } // aktivan, zavrsen
        public double Razdaljina { get; set; }

        public double StartX { get; set; }
        public double StartY { get; set; }
        public double EndX { get; set; }
        public double EndY { get; set; }

        public Zadatak()
        {
            Status = "aktivan";
        }

        public byte[] ToBytes()
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream())
            {
                bf.Serialize(ms, this);
                return ms.ToArray();
            }
        }

        public static Zadatak FromBytes(byte[] data)
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream(data))
            {
                return (Zadatak)bf.Deserialize(ms);
            }
        }
    }
}
