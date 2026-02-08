using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Domain;

namespace Server
{
    class Server
    {
        static int zadatakIdCounter = 1;
        static int klijentIdCounter = 1;

        static Socket tcpListener;
        static Socket udpServer;

        const int TCP_PORT = 5000;
        const int UDP_PORT = 5001;
        const double CENA_PO_KM = 50.0;
        const double BRZINA = 1.0;

        static void Main(string[] args)
        {
            Console.WriteLine("=== TAKSI CENTAR SERVER ===");
            Console.WriteLine($"TCP port (vozila): {TCP_PORT}");
            Console.WriteLine($"UDP port (klijenti): {UDP_PORT}");
            Console.WriteLine();

            // Kreiraj test vozila
            InicijalizujVozila();

            // Pokreni TCP listener za vozila (Socket)
            tcpListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            tcpListener.Bind(new IPEndPoint(IPAddress.Any, TCP_PORT));
            tcpListener.Listen(10);
            Console.WriteLine("TCP listener pokrenut - ceka vozila...");

            // Pokreni UDP soket za klijente (Socket)
            udpServer = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            udpServer.Bind(new IPEndPoint(IPAddress.Any, UDP_PORT));
            Console.WriteLine("UDP soket pokrenut - ceka zahteve klijenata...");
            Console.WriteLine();

            // Pokreni nit za prihvatanje TCP konekcija vozila
            Thread tcpThread = new Thread(PrihvatiVozila);
            tcpThread.IsBackground = true;
            tcpThread.Start();

            // Glavni polling loop
            while (true)
            {
                // Proveri UDP zahteve od klijenata
                ObradiUdpZahteve();

                // Simuliraj kretanje vozila
                SimulirajKretanje();

                // Prikazi stanje sistema
                PrikaziStanje();

                Thread.Sleep(1000);
            }
        }
    }
}
