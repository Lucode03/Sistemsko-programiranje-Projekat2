﻿using Kes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Projekat2_Zadatak7_2.Kes.KorisceniKesevi
{
    internal class KesZaSlike
    {
        protected readonly OptimizovanLRUKes<string, byte[]> _kes;
        protected readonly string _rootFolder;

        public KesZaSlike(string rootFolder)
        {
            _rootFolder = rootFolder;
            _kes = new OptimizovanLRUKes<string, byte[]>(1000);
        }

        virtual public async Task<byte[]?> PribaviSlikuAsync(string nazivFajla)
        {
            byte[] podaci;
            if (_kes.ProbajDaPribavisVrednost(nazivFajla, out podaci))
                return podaci;

            string? putanjaDoFajla = await NadjiFajlAsync(nazivFajla);
            if (putanjaDoFajla == null)// da li da ovde stavimo odmah return ili da izbacimo
                return null;
            //podaci = putanjaDoFajla == null ? null :await  File.ReadAllBytesAsync(putanjaDoFajla);
            podaci= await File.ReadAllBytesAsync(putanjaDoFajla);
            _kes.DodajIliAzuriraj(nazivFajla, podaci);
            return podaci;
        }

        protected async Task<string?> NadjiFajlAsync(string fileName)
        {
            try
            {
                return await Task.Run(() =>
                {
                    foreach (var file in Directory.EnumerateFiles(_rootFolder, "*", SearchOption.AllDirectories))
                    {
                        if (Path.GetFileName(file).Equals(fileName, StringComparison.OrdinalIgnoreCase))
                            return file;
                    }
                    return null;
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"Desila se greska u neautorizovanom pristupu fajlu: {ex.Message}");
            }
            catch (PathTooLongException ex)
            {
                Console.WriteLine($"Putanja do fajla je preduga: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Desila se neočekivana greška: {ex.Message}");
            }

            return null;
        }

        public void PisiRecnik()
        {
            _kes.PisiKes();
        }
    }
}
