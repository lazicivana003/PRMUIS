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
        static List<TaksiVozilo> vozila = new List<TaksiVozilo>();
        static List<Klijent> klijenti = new List<Klijent>();
        static List<Zadatak> zadaci = new List<Zadatak>();
        static List<Socket> tcpKlijenti = new List<Socket>();

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

        static void InicijalizujVozila()
        {
            Random rng = new Random();
            for (int i = 1; i <= 5; i++)
            {
                vozila.Add(new TaksiVozilo
                {
                    Id = i,
                    X = rng.Next(0, 20),
                    Y = rng.Next(0, 20),
                    Status = "slobodno"
                });
            }
            Console.WriteLine($"Inicijalizovano {vozila.Count} vozila.");
        }

        static void PrihvatiVozila()
        {
            while (true)
            {
                try
                {
                    Socket client = tcpListener.Accept();
                    lock (tcpKlijenti)
                    {
                        tcpKlijenti.Add(client);
                    }
                    Console.WriteLine("Novo vozilo povezano preko TCP-a.");
                }
                catch { }
            }
        }

        static void ObradiUdpZahteve()
        {
            while (udpServer.Available > 0)
            {
                try
                {
                    byte[] buffer = new byte[4096];
                    EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                    int primljeno = udpServer.ReceiveFrom(buffer, ref remoteEP);
                    byte[] data = new byte[primljeno];
                    Array.Copy(buffer, data, primljeno);

                    Klijent klijent = Klijent.FromBytes(data);
                    klijent.Id = klijentIdCounter++;
                    klijent.Status = "cekanje";
                    klijenti.Add(klijent);

                    Console.WriteLine($"Primljen zahtev od klijenta {klijent.Id}: ({klijent.StartX},{klijent.StartY}) -> ({klijent.EndX},{klijent.EndY})");

                    // Pronadji najblize slobodno vozilo
                    TaksiVozilo najblize = PronadjiNajblizeVozilo(klijent.StartX, klijent.StartY);

                    if (najblize != null)
                    {
                        // Kreiraj zadatak
                        Zadatak z = new Zadatak
                        {
                            Id = zadatakIdCounter++,
                            KlijentId = klijent.Id,
                            VoziloId = najblize.Id,
                            Status = "aktivan",
                            StartX = klijent.StartX,
                            StartY = klijent.StartY,
                            EndX = klijent.EndX,
                            EndY = klijent.EndY
                        };
                        zadaci.Add(z);

                        najblize.Status = "odlazak";
                        klijent.Status = "prihvaceno";

                        double udaljenost = IzracunajRazdaljinu(najblize.X, najblize.Y, klijent.StartX, klijent.StartY);
                        double eta = udaljenost / BRZINA;

                        Console.WriteLine($"Vozilo {najblize.Id} dodeljeno klijentu {klijent.Id}. ETA: {eta:F1}s");

                        // Posalji odgovor klijentu putem UDP-a
                        string odgovor = $"Zahtev prihvacen. Vozilo {najblize.Id} dolazi. ETA: {eta:F1}s";
                        byte[] odgovorBytes = System.Text.Encoding.UTF8.GetBytes(odgovor);
                        udpServer.SendTo(odgovorBytes, remoteEP);

                        // Posalji zadatak vozilu putem TCP-a ako je povezano
                        PosaljiZadatakVozilu(z);
                    }
                    else
                    {
                        string odgovor = "Nema slobodnih vozila. Sacekajte.";
                        byte[] odgovorBytes = System.Text.Encoding.UTF8.GetBytes(odgovor);
                        udpServer.SendTo(odgovorBytes, remoteEP);
                        Console.WriteLine("Nema slobodnih vozila za klijenta " + klijent.Id);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Greska pri obradi UDP zahteva: " + ex.Message);
                }
            }
        }

        static TaksiVozilo PronadjiNajblizeVozilo(double x, double y)
        {
            TaksiVozilo najblize = null;
            double minRazdaljina = double.MaxValue;

            foreach (var v in vozila)
            {
                if (v.Status == "slobodno")
                {
                    double r = IzracunajRazdaljinu(v.X, v.Y, x, y);
                    if (r < minRazdaljina)
                    {
                        minRazdaljina = r;
                        najblize = v;
                    }
                }
            }
            return najblize;
        }

        static double IzracunajRazdaljinu(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
        }

        static void PosaljiZadatakVozilu(Zadatak z)
        {
            lock (tcpKlijenti)
            {
                foreach (var tc in tcpKlijenti)
                {
                    try
                    {
                        if (tc.Connected)
                        {
                            byte[] data = z.ToBytes();
                            byte[] lenBytes = BitConverter.GetBytes(data.Length);
                            tc.Send(lenBytes);
                            tc.Send(data);
                        }
                    }
                    catch { }
                }
            }
        }

        static void SimulirajKretanje()
        {
            foreach (var z in zadaci.ToList())
            {
                if (z.Status != "aktivan") continue;

                TaksiVozilo v = vozila.FirstOrDefault(vz => vz.Id == z.VoziloId);
                Klijent k = klijenti.FirstOrDefault(kl => kl.Id == z.KlijentId);
                if (v == null || k == null) continue;

                double ciljX, ciljY;

                if (v.Status == "odlazak")
                {
                    ciljX = z.StartX;
                    ciljY = z.StartY;
                }
                else // voznja
                {
                    ciljX = z.EndX;
                    ciljY = z.EndY;
                }

                double dist = IzracunajRazdaljinu(v.X, v.Y, ciljX, ciljY);

                if (dist <= BRZINA)
                {
                    double predjeno = dist;
                    v.X = ciljX;
                    v.Y = ciljY;
                    v.Kilometraza += predjeno;

                    if (v.Status == "odlazak")
                    {
                        v.Status = "voznja";
                        Console.WriteLine($"Vozilo {v.Id} stiglo do klijenta {k.Id}. Pocinje voznja.");
                    }
                    else
                    {
                        // Zavrsena voznja
                        double razdaljina = IzracunajRazdaljinu(z.StartX, z.StartY, z.EndX, z.EndY);
                        double zarada = razdaljina * CENA_PO_KM;

                        v.Status = "slobodno";
                        v.Zarada += zarada;
                        v.BrojMusterija++;
                        v.Kilometraza += razdaljina;

                        z.Status = "zavrsen";
                        z.Razdaljina = razdaljina;

                        k.Status = "zavrseno";

                        Console.WriteLine($"Voznja zavrsena! Vozilo {v.Id}, Klijent {k.Id}, Razdaljina: {razdaljina:F1}km, Zarada: {zarada:F1}din");
                    }
                }
                else
                {
                    // Pomeri vozilo prema cilju
                    double dx = ciljX - v.X;
                    double dy = ciljY - v.Y;
                    v.X += (dx / dist) * BRZINA;
                    v.Y += (dy / dist) * BRZINA;
                    v.Kilometraza += BRZINA;
                }
            }
        }

        static void PrikaziStanje()
        {
            Console.Clear();
            Console.WriteLine("╔══════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                      TAKSI CENTAR SERVER                         ║");
            Console.WriteLine("╠══════════════════════════════════════════════════════════════════╣");
            Console.WriteLine();

            // Tabela vozila
            Console.WriteLine("  VOZILA:");
            Console.WriteLine("  ┌────┬────────────┬──────────┬──────────────┬────────────┬──────────┐");
            Console.WriteLine("  │ ID │  Pozicija  │  Status  │ Kilometraza  │   Zarada   │ Musterije│");
            Console.WriteLine("  ├────┼────────────┼──────────┼──────────────┼────────────┼──────────┤");
            foreach (var v in vozila)
            {
                Console.WriteLine($"  │{v.Id,3} │ ({v.X,4:F1},{v.Y,4:F1})│{v.Status,9} │{v.Kilometraza,13:F1} │{v.Zarada,11:F1} │{v.BrojMusterija,9} │");
            }
            Console.WriteLine("  └────┴────────────┴──────────┴──────────────┴────────────┴──────────┘");
            Console.WriteLine();

            // Tabela zahteva klijenata
            var aktivniKlijenti = klijenti.Where(k => k.Status != "zavrseno").ToList();
            Console.WriteLine("  AKTIVNI ZAHTEVI KLIJENATA:");
            Console.WriteLine("  ┌────┬──────────────┬──────────────┬────────────┐");
            Console.WriteLine("  │ ID │   Pocetna    │   Krajnja    │   Status   │");
            Console.WriteLine("  ├────┼──────────────┼──────────────┼────────────┤");
            if (aktivniKlijenti.Count == 0)
            {
                Console.WriteLine("  │         Nema aktivnih zahteva.              │");
            }
            else
            {
                foreach (var k in aktivniKlijenti)
                {
                    Console.WriteLine($"  │{k.Id,3} │ ({k.StartX,4:F1},{k.StartY,4:F1}) │ ({k.EndX,4:F1},{k.EndY,4:F1}) │{k.Status,11} │");
                }
            }
            Console.WriteLine("  └────┴──────────────┴──────────────┴────────────┘");
            Console.WriteLine();

            // Tabela zadataka
            var aktivniZadaci = zadaci.Where(z => z.Status == "aktivan").ToList();
            Console.WriteLine($"  Aktivnih zadataka: {aktivniZadaci.Count}  |  Ukupno zavrsenih: {zadaci.Count(z => z.Status == "zavrsen")}");
            Console.WriteLine();
            Console.WriteLine("  Cekam zahteve na UDP portu " + UDP_PORT + "...");
        }
    }
}
