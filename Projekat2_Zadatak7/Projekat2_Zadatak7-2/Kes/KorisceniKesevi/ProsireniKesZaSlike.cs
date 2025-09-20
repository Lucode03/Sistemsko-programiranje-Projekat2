using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Projekat2_Zadatak7_2.Kes.KorisceniKesevi
{
    internal class ProsireniKesZaSlike:KesZaSlike
    {
        private readonly ConcurrentDictionary<string, Lazy<Task<byte[]>>> _taskoviUToku;

        public ProsireniKesZaSlike(string rootFolder) : base(rootFolder)
        {
            _taskoviUToku = new ConcurrentDictionary<string, Lazy<Task<byte[]>>>();
        }

        public override async Task<byte[]?> PribaviSlikuAsync(string nazivFajla)
        {
            byte[] podaci;
            if (_kes.ProbajDaPribavisVrednost(nazivFajla, out podaci))
                return podaci;

            // Koristimo Lazy<Task> za sinhronizaciju pristupa disku
            var lazyTask = _taskoviUToku.GetOrAdd(nazivFajla, key =>
                new Lazy<Task<byte[]>>(async () =>
                {
                    try
                    {
                        string? putanjaDoFajla = await NadjiFajlAsync(key);
                        if (putanjaDoFajla == null)
                            return null;

                        var podaci = await File.ReadAllBytesAsync(putanjaDoFajla);
                        _kes.DodajIliAzuriraj(key, podaci);
                        return podaci;
                    }
                    finally
                    {
                        // Uklanjamo task iz rečnika kada se završi
                        _taskoviUToku.TryRemove(key, out _);
                    }
                }));

            try
            {
                return await lazyTask.Value;
            }
            catch
            {
                // U slučaju greške, uklanjamo task iz rečnika
                _taskoviUToku.TryRemove(nazivFajla, out _);
                throw;
            }
        }
    }
}
