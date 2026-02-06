using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Domain
{
    [Serializable]
    public class TaksiVozilo
    {
        public int Id { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public string Status { get; set; } // slobodno, odlazak, voznja
        public double Kilometraza { get; set; }
        public double Zarada { get; set; }
        public int BrojMusterija { get; set; }

        public TaksiVozilo()
        {
            Status = "slobodno";
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

        public static TaksiVozilo FromBytes(byte[] data)
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream(data))
            {
                return (TaksiVozilo)bf.Deserialize(ms);
            }
        }
    }
}
