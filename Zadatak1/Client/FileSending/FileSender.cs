using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Common;


namespace Client.FileSending
{
    public class FileSender : IFileSender
    {
        // preko ovoga se poziva slanje fajlova i dobija se rezultat 
        private readonly IFileHandling proxy;
        public FileSender(IFileHandling proxy)
        {
            this.proxy = proxy;
        }

        private void Ocisti()
        {
            MessageBox.Show("Fajlovi obradjeni");
            Path.Path.selectedPath = null;
            Path.Path.FilePaths = null;
            Path.Path.fileNames.Clear();
            ((MainWindow)System.Windows.Application.Current.MainWindow).allFiles.Items.Clear();
        }
        /// <summary>
        /// Za taj odredjeni izracunati fajl pretvara ga u memori strim objekat i upisuje kad na odredjenom putanji koja je zadata u ResultsFolder
        /// </summary>
        /// <param name="fajl"></param>
        /// <param name="ResultsFolder"></param>
        private void CuvanjeFajla(CalculatedFile fajl, DirectoryInfo ResultsFolder)
        {
            fajl.MS.Position = 0;
            StreamWriter sw = new StreamWriter(fajl.MS);
            var fs = new FileStream($"{ResultsFolder.FullName}\\{fajl.FileName}", FileMode.Create, FileAccess.Write);
            fajl.MS.WriteTo(fs);
            fs.Close();
            fs.Dispose();
            fajl.Dispose();
            //ciscenje objekta odmah da ne mora da prodju 2 ciklusa 
        }

        /// <summary>
        /// Ova fja pakuje u FileOverNetwork 
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="listOfFiles"></param>
        private void FONLista(string filePath, List<FileOverNetwork> listOfFiles)
        {
            var fileName = System.IO.Path.GetFileName(filePath);
            //Uzima fajl sa odredjene putanje i njega prepakuje u memory strim i kasnije sve to u okviru konstruktora u FileOverNetwork 
            FileOverNetwork fon = new FileOverNetwork(GetMemoryStream(filePath), fileName);
            listOfFiles.Add(fon);
            // na kraju dodaje u tu listu fajlova koju saljemo 
        }

        /// <summary>
        /// Fja prima filePathove 
        /// </summary>
        /// <param name="files"></param>
        public void SendFiles(string[] files)
        {
            List<FileOverNetwork> listOfFiles = new List<FileOverNetwork>();
            //Prolazi kroz sve filePathove i onda za svaki od njih pojedinacno pravi FileOverNetwork i dodaje u gornju listu
            foreach (string filePath in files)
            {
                FONLista(filePath, listOfFiles);
            }

            var res = proxy.SendFiles(listOfFiles); // onda ovde posalje te fajlove, u resu se cuva List<CalculatedFile>
            DirectoryInfo ResultsFolder;
            //ako ne postoji napravi 
            if (!Directory.Exists($"{Path.Path.selectedPath}\\results"))
            {
                ResultsFolder = Directory.CreateDirectory($"{Path.Path.selectedPath}\\results");
            }
            else
            {
               // ako postoji otvori 
                ResultsFolder = new DirectoryInfo($"{Path.Path.selectedPath}\\results");
            }
            foreach (CalculatedFile fajl in res)
            {
                CuvanjeFajla(fajl, ResultsFolder);
                // pogledaj gore kako radi ta fja 
            }
            Ocisti(); // javi da su stigli fajlovi i ocisti sve da mozemo da saljemo nove fajlove
        }

        /// <summary>
        /// Pravi MS objekat na osnovu filePatha otvara fileStrim koji pretvara u memory stream 
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private MemoryStream GetMemoryStream(string filePath)
        {
            MemoryStream ms = new MemoryStream();

            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                fileStream.CopyTo(ms);
                fileStream.Close();
            }
            return ms;
        }
    }
}
