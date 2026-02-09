# TaksiCentar

Sistem za simulaciju taksi centra sa centralnim serverom, klijentima i vozilima. Komunikacija se odvija putem TCP (vozila) i UDP (klijenti) protokola.

## Struktura projekta

```
TaksiCentar/
├── Domain/            - Zajednicke klase (biblioteka)
│   ├── TaksiVozilo.cs
│   ├── Klijent.cs
│   └── Zadatak.cs
├── Server/            - Centralni server (konzolna aplikacija)
│   └── Server.cs
├── Client/            - Klijentska aplikacija (konzolna aplikacija)
│   └── Client.cs
└── TaksiCentar.sln
```

## Zadatak 2: Inicijalizacija servera i osnovna komunikacija

**Fajl:** `Server/Server.cs`

- Server pokrece **TCP soket na portu 5000** za povezivanje vozila (`Socket` — `SocketType.Stream`, `ProtocolType.Tcp`).
- Server pokrece **UDP soket na portu 5001** za prijem zahteva od klijenata (`Socket` — `SocketType.Dgram`, `ProtocolType.Udp`).
- Klijent salje podatke (pocetna i krajnja tacka putovanja) putem UDP-a — metoda `ObradiUdpZahteve()`.
- Server prima podatke od vozila (pozicija, status) putem TCP-a — metoda `PrihvatiVozila()`.
- Testiranje: pokrenuti `Server.exe`, zatim `Client.exe`, uneti koordinate i proveriti da server prima zahtev i dodeljuje vozilo.

## Zadatak 3: Definicija klasa za taksi vozilo, klijenta i zadatak

**Fajlovi:** `Domain/TaksiVozilo.cs`, `Domain/Klijent.cs`, `Domain/Zadatak.cs`

### TaksiVozilo (`Domain/TaksiVozilo.cs`)
- `Id` — identifikator vozila
- `X`, `Y` — trenutne koordinate
- `Status` — slobodno, odlazak, voznja
- `Kilometraza` — ukupna predjena kilometraza
- `Zarada` — ukupna zarada
- `BrojMusterija` — ukupan broj prevezenih musterija

### Klijent (`Domain/Klijent.cs`)
- `Id` — identifikator klijenta
- `StartX`, `StartY` — pocetna tacka
- `EndX`, `EndY` — krajnja tacka
- `Status` — cekanje, prihvaceno, zavrseno

### Zadatak (`Domain/Zadatak.cs`)
- `Id` — identifikator zadatka
- `KlijentId`, `VoziloId` — vezani klijent i vozilo
- `Status` — aktivan, zavrsen
- `Razdaljina` — predjena razdaljina
- `StartX`, `StartY`, `EndX`, `EndY` — koordinate putovanja

Sve tri klase imaju `[Serializable]` atribut i metode `ToBytes()` / `FromBytes()` za serijalizaciju putem `MemoryStream` i `BinaryFormatter`.

## Zadatak 4: Osnovna simulacija dodavanja i izvrsavanja zadatka

**Fajl:** `Server/Server.cs`

1. Klijent salje zahtev za prevoz serveru putem UDP-a — `Client/Client.cs`, metoda `Main()`.
2. Server pronalazi najblize slobodno vozilo — metoda `PronadjiNajblizeVozilo()` (Euklidska razdaljina).
3. Server kreira `Zadatak` i salje ga vozilu putem TCP-a — metoda `PosaljiZadatakVozilu()`.
4. Vozilo prelazi u stanje "odlazak", pa "voznja" — metoda `SimulirajKretanje()`.
5. Server azurira status vozila i zadatka nakon zavrsetka — unutar `SimulirajKretanje()`.

## Zadatak 5: Upravljanje zadacima i raspodela zahteva

**Fajl:** `Server/Server.cs`

- **Polling model:** Glavni `while(true)` loop u `Main()` — svake sekunde obradjuje UDP poruke, simulira kretanje i osvezava prikaz.
- **Raspodela zadataka:** `PronadjiNajblizeVozilo()` bira najblize slobodno vozilo na osnovu Euklidske razdaljine.
- **Odgovor klijentu:** Server salje UDP odgovor sa potvrdnom i ETA (priblizno vreme dolaska) — unutar `ObradiUdpZahteve()`.
- **Dinamicko azuriranje:** Status vozila i zadatka se azurira u svakom ciklusu — `SimulirajKretanje()`.

### Vizuelizacija (`PrikaziStanje()`):
- Tabelarni prikaz svih vozila: pozicija, status, kilometraza, zarada, broj musterija.
- Tabelarni prikaz aktivnih zahteva klijenata: pocetna/krajnja tacka, status.
- Dinamicko osvezavanje svakog ciklusa (svake sekunde).

## Zadatak 6: Simulacija voznje sa pracenjem pozicija

**Fajl:** `Server/Server.cs`, metoda `SimulirajKretanje()`

- Vozilo se krece prema klijentovoj pocetnoj tacki (status "odlazak"), zatim prema krajnjoj tacki (status "voznja").
- Kretanje se simulira korak po korak (`BRZINA = 1.0` jedinica po ciklusu).
- Razdaljina se izracunava Euklidskom formulom — metoda `IzracunajRazdaljinu()`.
- Pozicije se azuriraju u svakom ciklusu i prikazuju u tabeli.

## Zadatak 7: Logika zavrsetka zadatka i azuriranje performansi

**Fajl:** `Server/Server.cs`, unutar `SimulirajKretanje()`

Po zavrsetku voznje (vozilo stiglo na krajnju tacku):
- Racuna se predjena razdaljina i zarada (`CENA_PO_KM = 50.0 din/km`).
- Vozilo se vraca u status "slobodno".
- Azurira se: `Zarada`, `Kilometraza`, `BrojMusterija` na vozilu.
- Zadatak se oznacava kao "zavrsen", klijent kao "zavrseno".
- Sve promene se prikazuju u tabelarnom prikazu na terminalu servera.

## Pokretanje

1. Buildovati solution u Visual Studio-u (ili MSBuild).
2. Pokrenuti `Server/bin/Debug/Server.exe`.
3. Pokrenuti `Client/bin/Debug/Client.exe` i uneti koordinate.
