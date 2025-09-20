using Projekat2_Zadatak7_2.Kes.KorisceniKesevi;
using System.Net;
using System.Text;

namespace Projekat2_Zadatak7
{
    internal class Server
    {
        private readonly HttpListener _listener;
        private readonly KesZaSlike _kesZaSlike;

        private readonly CountdownEvent _aktivniZahtevi;
        //Ovaj mehanizam mora da postoji kako bi imali uvid u 
        // to koliko se jos zahteva ceka. Ako to ne bismo imali,
        // kada bi prekinuli server, ne bi sacekao da se obrade ti zahtevi.
        private CancellationTokenSource _cts;
        private Task _listenerTask;

        public Server(string rootFolder, string urlPrefix)
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add(urlPrefix);
            _kesZaSlike = new ProsireniKesZaSlike(rootFolder);
            _aktivniZahtevi = new CountdownEvent(1);
        }

        public void StartAsync()
        {
            ConsoleInfo.Server("Server je startovan.");
            _cts = new CancellationTokenSource();
            _listenerTask = OsluskujZahteveAsync(_cts.Token);
        }

        public async Task StopAsync()
        {
            ConsoleInfo.Server("Server ce biti zaustavljen cim se zavrse svi aktivni zahtevi...");
            _aktivniZahtevi.Signal(); // Ukidamo onaj jedan sto smo inicijalno postavili.
            _cts.Cancel();
            await _listenerTask;
            _aktivniZahtevi.Wait(); // Cekamo da se zavrse svi tekuci zahtevi.
            _listener.Stop();
            ConsoleInfo.Server("Server je zaustavljen.");
        }

        private async Task OsluskujZahteveAsync(CancellationToken token)
        {
            _listener.Start();
            try
            {
                while (!token.IsCancellationRequested)
                {
                    /*var getContextTask = _listener.GetContextAsync();
                    var completed = await Task.WhenAny(getContextTask, Task.Delay(-1, token));
                    // Ako se u medjuvremenu ne pojavi zahtev, a server se zaustavi,
                    // onda ce da se prekine i ovo cekanje.
                    // WhenAny ceka da se zavrsi jedadn ili drugi task.

                    if (completed != getContextTask)
                        break;

                    var context = await getContextTask;
                    _ = ObradiZahtevAsync(context);*/ // Problem je sto kada ovaj Task.Delay(-1, token) se pokrene,
                    // on ce nastaviti da postoji cak i kada se pojavi context, i onda tako za svaki novi zahtev ce da 
                    // postoji taj jedan task koji u sustini ne radi nista.

                    /*var getContextTask = _listener.GetContextAsync();
                    var context = await getContextTask.WaitAsync(token);
                    _ = ObradiZahtevAsync(context);*/
                    //Ovde je problem sto ako se samo ostavi ovako, onda se nece sacekati zahtevi koji se 
                    // trenutno obradjuju. Mora na neki nacin da se vodi evidencija o tim zahtevima.

                    var context = await _listener.GetContextAsync().WaitAsync(token);

                    // Povećavamo broj aktivnih zahteva
                    _aktivniZahtevi.AddCount();

                    // Pokrećemo obradu zahteva
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await ObradiZahtevAsync(context);
                        }
                        finally
                        {
                            // Smanjujemo broj aktivnih zahteva kada se zahtev završi
                            _aktivniZahtevi.Signal();
                        }
                    });
                }
            }
            catch (HttpListenerException lEx)
            {
                //Console.WriteLine($"Desila se greska sa listenerom: {lEx.Message}");
                ConsoleInfo.Greska("Desila se greska sa listenerom", lEx);
            }
            catch(OperationCanceledException tEx)
            {
                if (token.IsCancellationRequested)
                    ConsoleInfo.Print("Listener je zaustavljen.");
                else
                    ConsoleInfo.Greska("Desila se greska pri radu sa taskom", tEx);
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Desila se greska na serveru: {ex.Message}");
                ConsoleInfo.Greska("Desila se greska na serveru", ex);
            }
            finally
            {
                //_listener.Stop(); 
            }
        }

        private async Task ObradiZahtevAsync(HttpListenerContext context)
        {
            string nazivFajla = context.Request.Url.AbsolutePath.TrimStart('/');
            if (nazivFajla == "favicon.ico")
            {
                await PosaljiOdgovorAsync(context, 204, "No Content");
                return;
            }
            //Console.WriteLine($"[LOG] Zahtev primljen: {nazivFajla} | Vreme: {DateTime.Now}");
            ConsoleInfo.Log("Zahtev primljen", nazivFajla);
            if (string.IsNullOrWhiteSpace(nazivFajla))
            {
                //Console.WriteLine("[GRESKA] Lose postavljen zahtev.");
                ConsoleInfo.Greska("Lose postavljen zahtev.");
                await PosaljiOdgovorAsync(context, 400, "Bad Request.");
                return;
            }
            if (context.Request.HttpMethod.ToLower() != "get")
            {
                //Console.WriteLine("[GRESKA] Losa metoda zahteva (metoda mora biti GET).");
                ConsoleInfo.Greska("Losa metoda zahteva (metoda mora biti GET).");
                await PosaljiOdgovorAsync(context, 400, "Bad Request.");
                return;
            }
            if (!Ekstenzije.ValidnaEkstenzija(nazivFajla))
            {
                //Console.WriteLine("[GRESKA] Losa ekstenzija trazenog fajla.");
                ConsoleInfo.Greska("Losa ekstenzija trazenog fajla.");
                await PosaljiOdgovorAsync(context, 400, "Bad Request.");
                return;
            }
            try
            {
                var podacoOSlici = await _kesZaSlike.PribaviSlikuAsync(nazivFajla);
                if (podacoOSlici == null)
                {
                    //Console.WriteLine($"[INFO] Fajl nije pronadjen: {nazivFajla}");
                    ConsoleInfo.Info("Fajl nije pronadjen", nazivFajla);
                    await PosaljiOdgovorAsync(context, 404, $"Image '{nazivFajla}' Not Found.");
                    return;
                }

                context.Response.ContentType = Ekstenzije.VratiMimeTip(nazivFajla);
                context.Response.ContentLength64 = podacoOSlici.Length;
                await context.Response.OutputStream.WriteAsync(podacoOSlici, 0, podacoOSlici.Length);
                await context.Response.OutputStream.FlushAsync();
                context.Response.OutputStream.Close();

                //Console.WriteLine($"[INFO] Isporucena slika: {nazivFajla}");
                ConsoleInfo.Info("Isporucena slika", nazivFajla);
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Greska: {ex.Message}");
                ConsoleInfo.Greska("Greska",ex);
                await PosaljiOdgovorAsync(context, 500, "Internal Server Error.");
            }
            /*finally
            {
                //_kesZaSlike.PisiRecnik();
            }*/
        }

        private async Task PosaljiOdgovorAsync(HttpListenerContext context, int statusKod, string poruka)
        {
            context.Response.StatusCode = statusKod;
            byte[] buffer = Encoding.UTF8.GetBytes(poruka);
            context.Response.ContentType = "text/plain";
            context.Response.ContentLength64 = buffer.Length;
            await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            await context.Response.OutputStream.FlushAsync();
            context.Response.OutputStream.Close();
        }
    }
}
