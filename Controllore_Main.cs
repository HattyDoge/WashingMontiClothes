using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Esempio_Controllore_Semafori
{
    internal class Esempio_Controllore_Semafori
    {
        // DA NON MODIFICARE: inizio
        static Semafori.frmSemafori fSemafori;
        static gamon.IO.Digital.IoSimulation IO;
        // DA NON MODIFICARE: fine

        static short portaIn = 0;   // primi 8 bit di I/O (0 .. 7)
        static short portaOut1 = 1; // secondi 8 bit di I/O (8 .. 15)
        static short portaOut2 = 2; // terzi 8 bit di I/O (16 .. 23)
        static byte estOvestVerde = 0b0011_1000;
        static byte sudRosso = 0b0000_0010;
        static byte estOvestGiallo = 0b0001_1100;

        private static void MostraSimulazione()
        {
            // DA NON MODIFICARE: inizio
            // dalla .Show() il form non esce mai in questo thread
            fSemafori = new Semafori.frmSemafori();
            IO = (gamon.IO.Digital.IoSimulation)fSemafori.frmDigitalIO.Hardware;
            fSemafori.Show();
            // DA NON MODIFICARE: fine
        }
        static void Main(string[] args)
        {
            // DA NON MODIFICARE: inizio
            // fa partire il sistema simulato
            Thread t1 = new Thread(MostraSimulazione);
            t1.Start();
            Thread.Sleep(5000); // attende per dare tempo all'altro thread di inizializzare tutto
            // DA NON MODIFICARE: fine

            // semafori rossi: 
            IO.Out(portaOut1, 0b1000_0000);
            IO.Out(portaOut2, 0b0100_0011);

            Thread.Sleep(8000); // temporaneo per test: attende che arrivi qualcuno in coda

            IO.Out(portaOut1, 0b0011_1000);
            // spengo semaforo Sud
            IO.Out(portaOut2, sudRosso);

            while (true)
            {
                while ((IO.In(portaIn) & 0b0001_0000) == 0) ;
                StradaSecondariaOn();
                while ((IO.In(portaIn) & 0b0000_1000) != 0) ;
                StradaSecondariaOff();
            }
        }
        static void StradaSecondariaOn()
        {
            IO.Out(portaOut1, 0);
            IO.Out(portaOut2, 0b0001_1110);
            Thread.Sleep(2000);
            IO.Out(portaOut1, 0b1100_0000);
            IO.Out(portaOut2, 0b0100_0001);
        }
        static void StradaSecondariaOff()
        {
            IO.Out(portaOut1, 0b1000_0000);
            IO.Out(portaOut2, 0b1010_0001);
            Thread.Sleep(2000);
            IO.Out(portaOut2, sudRosso);
            IO.Out(portaOut1, estOvestVerde);
        }
    }
}
