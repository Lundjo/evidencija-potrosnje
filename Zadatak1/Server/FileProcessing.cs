using Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Server
{
    public class FileProcessing
    {
        //pitaj stefana kako sta sve 
        public event EventHandler ListOfFilesProccessed;
        public event EventHandler<Audit> FileValidated;
        public event EventHandler<Load> RowProccessed;
        public event EventHandler<ImportedFile> StreamProccessed;

        private InMemoryDatabase baza = new InMemoryDatabase();
        private XMLDataBase xmlBaza = new XMLDataBase();
        //kupi iz app configa kako ce da radi program 
        string metodaProcesiranja = ConfigurationManager.AppSettings["calculationType"];
        private bool useXML = ConfigurationManager.AppSettings["storageType"].Equals("XMLDB") ? true : false;


        public List<CalculatedFile> ProcessFiles(List<FileOverNetwork> listOfFiles)
        {

            if (useXML)
            {
                ListOfFilesProccessed += xmlBaza.OnListOfFilesProccessed;
            }



            List<ImportedFile> processedFiles = new List<ImportedFile>(listOfFiles.Count);
            //toliko ce maks da bude procesiranih 
            foreach (FileOverNetwork fon in listOfFiles)
            {
                fon.MS.Seek(0, SeekOrigin.Begin); // pomera se na pocetak zbog citanja fajla 
                var strim = new StreamReader(fon.MS); // strim rider ce da cita to konkretno od pocetka 
                Audit izvestajZaFajl = ValidateFile(fon.FileName, strim);
                if (izvestajZaFajl.MessageType == MessageType.Info || izvestajZaFajl.MessageType == MessageType.Warning)
                {
                    processedFiles.Add(ProcessStream(strim, fon,izvestajZaFajl));
                    
                }
                strim.Dispose();
                fon.Dispose();
            }

            List<ImportedFile> kojiImajuObeVrednosti = FilesWithBothValues(processedFiles); // on ima po jedan imported file za datum ciji Loadovi imaju obe vrednosti

            processedFiles.Clear(); // ne trebaju vise 
            List<CalculatedFile> results = new List<CalculatedFile>(kojiImajuObeVrednosti.Count);
            foreach (ImportedFile file in kojiImajuObeVrednosti)
            {
                List<Load> loads = LoadsFromFile(file);
                WriteLoadsToFile(loads, file, results);
            }
            if (useXML)
            {
                OnListOfFilesProccessed();
                ListOfFilesProccessed -= xmlBaza.OnListOfFilesProccessed;
            }
            return results;
        }
        /// <summary>
        /// Imported file treba da bi mogao da izvuce datum 
        /// 
        /// </summary>
        /// <param name="loads"></param>
        /// <param name="file"></param>
        /// <param name="results"></param>

        private void WriteLoadsToFile(List<Load> loads, ImportedFile file, List<CalculatedFile> results)
        {
            MemoryStream ms = new MemoryStream();
            StreamWriter sw = new StreamWriter(ms);
            string header = metodaProcesiranja == "Squared" ? "SQUARED_DEVIATION" : "ABSOLUTE_PERCENTAGE_DEVIATION";
            sw.WriteLine("TIMESTAMP," + header);
            foreach (Load l in loads)
            {
                double rezultat = Izracunaj(l, metodaProcesiranja);
                sw.Write(l.TimeStamp.ToString("yyyy-MM-dd HH:mm") + "," + rezultat.ToString() + '\n');
            }
            sw.Flush(); // za svaki slucaj da ocisti writer 
            string resultFileName = "result_" + TakeDate(file.Filename).ToString("yyyy_MM_dd") + ".csv";
            results.Add(new CalculatedFile(ms, resultFileName));
            loads.Clear();
        }

        /// <summary>
        /// Krajnji datum je ustv dan pored ovog datuma iz fajla 
        /// I onda uzima loadove za svaki sat do kraja tog naseg dana iz fajla name-a 
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private List<Load> LoadsFromFile(ImportedFile file)
        {
            List<Load> loads = new List<Load>();
            DateTime datumIzFajla = TakeDate(file.Filename).Date;
            DateTime kranjiDatum = datumIzFajla.AddDays(1);
            while (datumIzFajla != kranjiDatum)
            {
                Load load = baza.GetLoad(datumIzFajla.GetHashCode());
                if (load != null) loads.Add(load);
                datumIzFajla = datumIzFajla.AddHours(1);
            }
            return loads;
        }

        /// <summary>
        /// Provera da li u listi ImportedFiles postoje dva objekta ciji se datumi poklapaju i to nam znaci da su oba ispravna i da imaju obe vrednosti u jednom Loadu
        /// </summary>
        /// <param name="processedFiles"></param>
        /// <returns></returns>
        private List<ImportedFile> FilesWithBothValues(List<ImportedFile> processedFiles)
        {
            List<ImportedFile> fwbv = new List<ImportedFile>();
            for (int i = 0; i < processedFiles.Count; ++i)
            {
                for (int j = i + 1; j < processedFiles.Count; ++j)
                {
                    if (TakeDate(processedFiles[i].Filename) == TakeDate(processedFiles[j].Filename))
                    {
                        fwbv.Add(processedFiles[i]);
                        break;
                    }
                }
            }
            return fwbv;
        }

        private double Izracunaj(Load l, string metodaProcesiranja)
        {
            double ostvarena = l.MeasuredValue;
            double prognozirana = l.ForecastValue;
            if (metodaProcesiranja == "Squared")
            {
                l.SquaredDeviation = Math.Pow(((ostvarena - prognozirana) / ostvarena), 2);
                return l.SquaredDeviation;
            }
            else
            {
                l.AbsolutePercentageDeviation = ((Math.Abs(ostvarena - prognozirana)) / ostvarena) * 100;
                return l.AbsolutePercentageDeviation;
            }
        }

        private Audit PovratniAudit(MessageType mt, string fileName, string poruka)
        {
            Audit audit = new Audit(DateTime.Now, mt, "Fajl: " + fileName + " - " + poruka);
            OnFileValidated(audit);
            FileValidated -= baza.OnFileValidated;
            return audit;
        }

        private Audit ProveraIspravnosti(StreamReader strim, string dateString, int num_hours, int hours, string fileName, bool ispravan)
        {

            while (!strim.EndOfStream)
            {
                string row = strim.ReadLine();
                string rowDate = RowDate(row);

                if (rowDate.Equals(dateString))
                {
                    if (ColoumnCheck(row))
                    {

                        num_hours++;
                    }
                    else
                    {
                        return PovratniAudit(MessageType.Error, fileName, "Broj elemenata u vrsti je veci od dozvoljenog");
                    }
                }
                else
                {
                    return PovratniAudit(MessageType.Error, fileName, "FileName i Date se ne poklapaju");
                }
            }
            if (!(num_hours == hours))
            {
                return PovratniAudit(MessageType.Error, fileName, "Los broj sati za fajl");
            }
            else
            {
                if (ispravan)
                {
                    return PovratniAudit(MessageType.Info, fileName, "Ispravan");
                }
                else
                {
                    return PovratniAudit(MessageType.Warning, fileName, "Pogresno definisan header");
                }
            }
        }

        private Audit ValidateFile(string fileName, StreamReader strim)
        {
            FileValidated += baza.OnFileValidated; // enable zapis u in memory bazu 
            string dateString = fnToDate(fileName); //prodjeno 
            DateTime time = TakeDate(fileName); // prodjeno 
            int month = TakeMonth(fileName); // 
            int year = TakeYear(fileName); // 
            int hours = CheckHours(month, LastSundayOfMonth(year, month), time);
            int num_hours = 0;
            string header = strim.ReadLine();

            if (HeaderCheck(header))
            {
                return ProveraIspravnosti(strim, dateString, num_hours, hours, fileName, true);
            }
            else
            {
                return ProveraIspravnosti(strim, dateString, num_hours, hours, fileName, false);
            }
        }

        /// <summary>
        /// Uzima red po red za neki fajl i pravi Loadove ostalo procitaj sam nisi retardiran 
        /// Malo odstupa od dijagram zato sto se Imported File kreira tek na kraju ali nista ne remeti rad programa 
        /// Znaci na kraju da se sve vrste iz fajla obrade 
        /// </summary>
        /// <param name="sr"></param>
        /// <param name="fon"></param>
        /// <param name="audit"></param>
        /// <returns></returns>
        private ImportedFile ProcessStream(StreamReader sr, FileOverNetwork fon,Audit audit)
        {
            RowProccessed += baza.OnRowProccessed; // da moze jedan taj load da upise u bazu 
            StreamProccessed += baza.OnStreamProccessed;
            sr.BaseStream.Seek(0, SeekOrigin.Begin);
            if (audit.MessageType == MessageType.Info)
            {
                sr.ReadLine(); // ako je ispravno cita heder i preskace ga 
            }
            ImportedFile trenutniImpf = new ImportedFile(fon.FileName);
            while (!sr.EndOfStream)
            {
                string row = sr.ReadLine();
                string rowDateTime = row.Split(',')[0];
                DateTime dateTime = DateTime.Parse(rowDateTime);
                Load load = baza.GetLoad(dateTime.GetHashCode());
                if (load == null)
                {
                    load = new Load(dateTime);
                }
                if (fon.GetFileType() == FileType.FORECAST)
                {

                    load.ForecastValue = double.Parse(row.Split(',')[1]);
                    load.ForecastFileID = trenutniImpf.Id;
                }
                else
                {
                    load.MeasuredValue = double.Parse(row.Split(',')[1]);
                    load.MeasuredFileID = trenutniImpf.Id;
                }

                OnRowProccessed(load); // na kraju pozove da ih upise sve 
            }
            RowProccessed -= baza.OnRowProccessed;

            OnStreamProccessed(trenutniImpf);
            StreamProccessed -= baza.OnStreamProccessed;
            return trenutniImpf;
        }

        private bool HeaderCheck(string header)
        {
            string[] zaglavlje = header.Split(',');
            string timeStamp = zaglavlje[0];
            string forecast_measured = zaglavlje[1];
            if (timeStamp.Equals("TIME_STAMP") &&
               (forecast_measured.Equals("FORECAST_VALUE") || forecast_measured.Equals("MEASURED_VALUE")))
                return true;
            else return false;
        }

        private bool ColoumnCheck(string col)
        {
            string[] colOne = col.Split(',');
            if (colOne.Length == 2) return true;
            else return false;

        }

        private string[] Dates(string fn)
        {
            // da izvuce .csv 
            // ostaje ti forecast_2023_01_01 
            string[] FileNameTimeStamp = fn.Split('.');
            //ovde vrati niz gde je nulti el forecast 
            // prvi 2023 
            // drugi 01 
            //treci 01 
            return FileNameTimeStamp[0].Split('_');
        }

        //izvlaci  2023-01-01 
        private string fnToDate(string fn)
        {
            string[] date = Dates(fn); // pogledaj gore komentar 
            return date[1] + "-" + date[2] + "-" + date[3];
        }
        private DateTime TakeDate(string fn)
        {
            string[] date = Dates(fn); // [forecast] [2023] [01] [01] 
            string[] day_and_month = new string[] { date[1], date[2], date[3] }; 
            return new DateTime(Int32.Parse(day_and_month[0]), Int32.Parse(day_and_month[1]), Int32.Parse(day_and_month[2]));
        }

        private int TakeMonth(string fn)
        {
            string[] date = Dates(fn); // vrati samo forecast 2023 01 01 i onda uzme na indeksu 2 tj 01 
            return Int32.Parse(date[2]);
        }
        private int TakeYear(string fn)
        {
            string[] date = Dates(fn); // isto kao sva ova ostala govna jebo sam im mater 
            return Int32.Parse(date[1]);
        }
        private string RowDate(string row)
        {
            string[] splitedRow = row.Split(',');
            string[] dateTime = splitedRow[0].Split(' ');
            return dateTime[0];
        }

        public DateTime LastSundayOfMonth(int year, int month)
        {
            // ova linija vrati ceo datum samo sa poslednjim danom za mesec i godinu npr 
            //2023 01 31 
            DateTime lastDayOfMonth = new DateTime(year, month, DateTime.DaysInMonth(year, month));

            // petak pretvra u 5 utorak u 2 itd ,cast ima priorite 
            int daysToLastSunday = ((int)lastDayOfMonth.DayOfWeek - (int)DayOfWeek.Sunday + 7) % 7;
            // siftuje i vrati koja je poslednja NEDELJA KAO DAN U MESECU 
            return lastDayOfMonth.AddDays(-daysToLastSunday);
        }
        /// <summary>
        /// Prvi parametar mesec konkretnog fileNamea 
        /// Poslednja nedelja u mesecu 
        /// Full datum iz fajla
        /// 
        /// </summary>
        /// <param name="month"></param>
        /// <param name="lastSunday"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        public int CheckHours(int month, DateTime lastSunday, DateTime date)
        {
            if (month == 3 && date == lastSunday)
            {
                return 23;
            }
            else if (month == 10 && date == lastSunday)
            {
                return 25;
            }
            else
            {
                return 24;
            }
        }

        /// <summary>
        /// Ako bi bilo null niko nije pretplacen na event 
        /// Obavestava sve koji su pretplaceni na event,koji su pretplaceni i sta treba da odrade 
        /// </summary>
        protected virtual void OnListOfFilesProccessed()
        {
            if (ListOfFilesProccessed != null)
            {
                ListOfFilesProccessed(this, EventArgs.Empty);
            }
        }

        protected virtual void OnFileValidated(Audit a)
        {
            if (FileValidated != null)
            {
                FileValidated(this, a);
            }
        }

        protected virtual void OnRowProccessed(Load l)
        {
            if (RowProccessed != null)
            {
                RowProccessed(this, l);
            }
        }

        protected virtual void OnStreamProccessed(ImportedFile iF)
        {
            if (StreamProccessed != null)
            {
                StreamProccessed(this, iF);
            }
        }
    }
}
