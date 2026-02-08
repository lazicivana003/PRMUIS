using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Domain;

namespace Client
{
    class Client
    {
        const int UDP_PORT = 5001;
        const string SERVER_IP = "127.0.0.1";

        static void Main(string[] args)
        {
            Console.WriteLine("=== TAKSI KLIJENT ===");
            Console.WriteLine();

            Socket udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            EndPoint serverEP = new IPEndPoint(IPAddress.Parse(SERVER_IP), UDP_PORT);

            while (true)
            {
                Console.WriteLine("Unesite koordinate za prevoz (ili 'exit' za izlaz):");

                Console.Write("Pocetna X: ");
                string input = Console.ReadLine();
                if (input.ToLower() == "exit") break;
                double startX = double.Parse(input);

                Console.Write("Pocetna Y: ");
                double startY = double.Parse(Console.ReadLine());

                Console.Write("Krajnja X: ");
                double endX = double.Parse(Console.ReadLine());

                Console.Write("Krajnja Y: ");
                double endY = double.Parse(Console.ReadLine());

                Klijent klijent = new Klijent
                {
                    StartX = startX,
                    StartY = startY,
                    EndX = endX,
                    EndY = endY,
                    Status = "cekanje"
                };

                // Posalji zahtev serveru putem UDP soketa
                byte[] data = klijent.ToBytes();
                udpSocket.SendTo(data, serverEP);
                Console.WriteLine("Zahtev poslat serveru...");

                // Primi odgovor od servera
                try
                {
                    udpSocket.ReceiveTimeout = 5000;
                    byte[] buffer = new byte[4096];
                    EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                    int primljeno = udpSocket.ReceiveFrom(buffer, ref remoteEP);
                    string odgovor = Encoding.UTF8.GetString(buffer, 0, primljeno);
                    Console.WriteLine($"Odgovor servera: {odgovor}");
                }
                catch (SocketException)
                {
                    Console.WriteLine("Server nije odgovorio u roku od 5 sekundi.");
                }

                Console.WriteLine();
            }

            udpSocket.Close();
            Console.WriteLine("Klijent zatvoren.");
        }
    }
}
