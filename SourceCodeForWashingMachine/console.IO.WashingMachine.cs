using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Windows.Forms;
using System.Threading;

namespace ConsoleTestSimulIO
{
    class Program
    {
        // DA NON MODIFICARE: inizio
        static Lavatrice.FrmLavatrice fLavatrice;
        static gamon.IO.Digital.IoSimulation IO;
        // DA NON MODIFICARE: fine

        static short portIn = 0;   // primi 8 bit di I/O (0 .. 7)
        static short portOut1 = 1; // secondi 8 bit di I/O (8 .. 15)
        static short portOut2 = 2; // terzi 8 bit di I/O (16 .. 23)
        static byte bloccaPortello = 0b0000_0001;

        private static void MostraSimulazione()
        {
            // DA NON MODIFICARE: inizio
            // dalla .Show() il form non esce mai in questo thread
            fLavatrice = new Lavatrice.FrmLavatrice();
            IO = (gamon.IO.Digital.IoSimulation)fLavatrice.frmDigitalIO.Hardware;
            fLavatrice.Show();
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

            // attesa che l'utente accenda la lavatrice: polling su bit accensione lavatrice
            // controlla se il bit "accensione" indica che la lavatrice è stata accesa
            while ((IO.In(portIn) & 0b0010_0000) == 0) { }; // esce daò ciclo dopo l'accensione

            // TODO (alla fine, prima fare il resto):
            // controlla che la porta sia chiusa
            if((IO.In(portIn) & bloccaPortello) == 0)
            {
                Console.WriteLine("Errore la porta non è chiusa !");
                while ((IO.In(portIn) & bloccaPortello) != 1) { Thread.Sleep(300); }
            }

            IO.Out(portOut2, bloccaPortello);
            // se è aperta, il controllore dà un allarme (a console) ed attende che l'utente la chiuda
            // se è chiusa la blocca 

            // lettura della bottoniera
            int nBottoniera = IO.In(portIn) & 0b1100_0000; // considera solo i DUE bit della bottoniera
            nBottoniera >>= 6; // porta a destra quei due bit (questa ve la spiego un'altra volta!) 
            switch (nBottoniera)
            {
                case 0: // prelavaggio
                    {
                        prelavaggio();
                        lavaggio();
                        risciacquo();
                        if ((IO.In(portIn) & 0b0000_0010) == 0b0000_0000)
                            centrifuga(); // farla solo se il bottone centrifuga è ON
                        break;
                    }
                case 1: // lavaggio
                    {
                        lavaggio();
                        risciacquo();
                        if ((IO.In(portIn) & 0b0000_0010) == 0b0000_0000)
                            centrifuga(); // farla solo se il bottone centrifuga è ON
                        break;
                    }
                case 3: // risciaquo
                    {
                        risciacquo();
                        if ((IO.In(portIn) & 0b0000_0010) == 0b0000_0000)
                            centrifuga();
                        // TODO completare
                        break;
                    }
                case 2: // centrifuga
                    {
                        centrifuga();
                        // fa la centrifuga, ANCHE SE il bottone centrifuga è OFF
                        // TODO completare
                        break;
                    }
            }
            // TODO apre il blocco della porta e spegne tutto,
            // per lasciare il sistema in condizione di sicurezza
        }
        static void controlloAcquaCalda()
        {
            if ((IO.In(portIn) & 0b0001_0000) == 0)
            {
                byte uscite1 = IO.In(portOut1);
                uscite1 |= 0b1000_0000;
                IO.Out(portOut1, uscite1);
            }
            else
            {
                byte uscite1 = IO.In(portOut1);
                uscite1 &= 0b0111_1111;
                IO.Out(portOut1, uscite1);
            }
        } 
        private static void prelavaggio()
        {
            // prelavaggio a freddo: non deve riscaldare l'acqua

            // prende l'acqua dalla vasca del prelavaggio
            // mascheramento con OR per accendere il bit 5 della PortaOut1 (sesto bit del secondo port) 
            byte uscite1 = IO.In(portOut1);
            uscite1 |= 0b0010_0000;
            IO.Out(portOut1, uscite1);

            // attende che l'acqua arrivi fino al livello alto
            while ((IO.In(portIn) & 0b0000_1000) == 0) { };

			//chiude entrata acqua
			uscite1 = IO.In(portOut1);
			uscite1 &= 0b1101_1111;
			IO.Out(portOut1, uscite1);

			//TODO gira piano in senso orario per 2 s
			for (int i = 0; i < 2; i++)
            {
                uscite1 = IO.In(portOut1);
                uscite1 |= 0b0000_0110;
                uscite1 &= 0b1111_1110;
                IO.Out(portOut1, uscite1);

                Thread.Sleep(2000);
                // ATTENDE 1/2 s
                uscite1 = IO.In(portOut1);
                uscite1 &= 0b1111_1001;
                IO.Out(portOut1, uscite1);
                Thread.Sleep(1000);
                //TODO gira piano in senso ANTIorario per 2 s
                uscite1 = IO.In(portOut1);
                uscite1 |= 0b0000_0100;
                IO.Out(portOut1, uscite1);

                Thread.Sleep(2000);
                uscite1 = IO.In(portOut1);
                uscite1 &= 0b1111_1011;
                IO.Out(portOut1, uscite1);
                Thread.Sleep(1000);
            }   //TODO ripete i giramenti per 2 volte

            // scarica l'acqua fino a che il livello non scende sotto il livello minimo
            uscite1 = IO.In(portOut1);
            uscite1 |= 0b0100_0000;
            IO.Out(portOut1,uscite1);

			while ((IO.In(portIn) & 0b0000_0100) != 0) { };
			// TODO da fare! 

			uscite1 = IO.In(portOut1);
			uscite1 &= 0b1011_1111;
			IO.Out(portOut1, uscite1);
			// !!!! TODO per fare MOLTO prima, bisognerebbe impostare un metodo, con i dovuti parametri,
			// !!!! che esegua un ciclo completo di "giramenti" del cestello
		}
        private static void lavaggio()
        {
			// lavaggio a caldo: riscalda l'acqua fino a raggiungere la temperatura di termostato
			byte uscite1 = IO.In(portOut1);
			uscite1 |= 0b0001_0000;
			IO.Out(portOut1, uscite1);

			// attende che l'acqua arrivi fino al livello alto
			while ((IO.In(portIn) & 0b0000_1000) == 0) { };
			//chiude entrata acqua
			uscite1 = IO.In(portOut1);
			uscite1 &= 0b1110_1111;
			IO.Out(portOut1, uscite1);
			// accende riscaldamento
			uscite1 = IO.In(portOut1);
            uscite1 |= 0b1000_0000;
			IO.Out(portOut1, uscite1);

            for (int i = 0; i < 5; i++)
            {
                uscite1 = IO.In(portOut1);
                uscite1 |= 0b0000_0110;
                uscite1 &= 0b1111_1110;
                IO.Out(portOut1, uscite1);

				controlloAcquaCalda();
				Thread.Sleep(5000);
                // ATTENDE 1/2 s
                uscite1 = IO.In(portOut1);
                uscite1 &= 0b1111_1001;
                IO.Out(portOut1, uscite1);
                Thread.Sleep(1000);
                //TODO gira piano in senso ANTIorario per 2 s
                uscite1 = IO.In(portOut1);
                uscite1 |= 0b0000_0100;
                IO.Out(portOut1, uscite1);

                controlloAcquaCalda();
                Thread.Sleep(5000);
                uscite1 = IO.In(portOut1);
                uscite1 &= 0b1111_1011;
                IO.Out(portOut1, uscite1);
                Thread.Sleep(1000);
            }

			// scarica l'acqua fino a che il livello non scende sotto il livello minimo
			uscite1 = IO.In(portOut1);
			uscite1 |= 0b0100_0000;
			IO.Out(portOut1, uscite1);

			// stacca il riscaldamento
			uscite1 = IO.In(portOut1);
			uscite1 &= 0b0111_1111;
			IO.Out(portOut1, uscite1);

			while ((IO.In(portIn) & 0b0000_0100) != 0 && (IO.In(portIn) & 0b0001_0000) != 0) { };

			uscite1 = IO.In(portOut1);
			uscite1 &= 0b1011_1111;
			IO.Out(portOut1, uscite1);
		}
        private static void risciacquo()
        {
            // identico a prelavaggio, ma prende l'acqua dalla vasca del risciaquo
            // lavaggio a caldo: riscalda l'acqua fino a raggiungere la temperatura di termostato
            byte uscite1 = IO.In(portOut1);
            uscite1 |= 0b0000_1000;
            IO.Out(portOut1, uscite1);

            // attende che l'acqua arrivi fino al livello alto
            while ((IO.In(portIn) & 0b0000_1000) == 0) { };

            //chiude entrata acqua
			uscite1 = IO.In(portOut1);
			uscite1 &= 0b1111_0111;
			IO.Out(portOut1, uscite1);

			// accende riscaldamento
			uscite1 = IO.In(portOut1);
            uscite1 |= 0b1000_0000;
            IO.Out(portOut1, uscite1);

            for (int i = 0; i < 3; i++)
            {
				uscite1 = IO.In(portOut1);
				uscite1 |= 0b0000_0110;
				uscite1 &= 0b1111_1110;
				IO.Out(portOut1, uscite1);

				controlloAcquaCalda();
                Thread.Sleep(3000);
                // ATTENDE 1/2 s
                uscite1 = IO.In(portOut1);
                uscite1 &= 0b1111_1001;
                IO.Out(portOut1, uscite1);
                Thread.Sleep(1000);
                //TODO gira piano in senso ANTIorario per 2 s
                uscite1 = IO.In(portOut1);
                uscite1 |= 0b0000_0100;
                IO.Out(portOut1, uscite1);

                controlloAcquaCalda();
                Thread.Sleep(3000);
                uscite1 = IO.In(portOut1);
                uscite1 &= 0b1111_1011;
                IO.Out(portOut1, uscite1);
                Thread.Sleep(1000);
            }
            // scarica l'acqua fino a che il livello non scende sotto il livello minimo
            uscite1 = IO.In(portOut1);
            uscite1 |= 0b0100_0000;
            IO.Out(portOut1, uscite1);
            // stacca il riscaldamento
            uscite1 = IO.In(portOut1);
            uscite1 &= 0b0111_1111;
            IO.Out(portOut1, uscite1);

            while ((IO.In(portIn) & 0b0000_0100) != 0 && (IO.In(portIn) & 0b0001_0000) != 0) { };

			uscite1 = IO.In(portOut1);
			uscite1 &= 0b1011_1111;
			IO.Out(portOut1, uscite1);
			// TODO 
		}
        private static void centrifuga()
        {
			byte uscite1 = IO.In(portOut1);
			uscite1 |= 0b0100_0001;
			IO.Out(portOut1, uscite1);

			for (int i = 0; i < 2; i++)
			{
				uscite1 = IO.In(portOut1);
				uscite1 |= 0b0000_0111;
				IO.Out(portOut1, uscite1);

				Thread.Sleep(7000);
				// ATTENDE 1/2 s
				uscite1 = IO.In(portOut1);
				uscite1 &= 0b1111_1001;
				IO.Out(portOut1, uscite1);
				Thread.Sleep(1000);
				//TODO gira piano in senso ANTIorario per 2 s
				uscite1 = IO.In(portOut1);
				uscite1 |= 0b0000_0100;
				IO.Out(portOut1, uscite1);

				Thread.Sleep(7000);
				uscite1 = IO.In(portOut1);
				uscite1 &= 0b1111_1011;
				IO.Out(portOut1, uscite1);
				Thread.Sleep(1000);
			}   //TODO ripete i giramenti per 2 volte
				// non prende acqua e lascia valvola di scarico aperta, per fare uscire l'acqua dai panni
				// la velocità di rotazione è alta e non bassa. 
		}
    }
}

