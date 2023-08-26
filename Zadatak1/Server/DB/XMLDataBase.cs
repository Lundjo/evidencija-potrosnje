using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Server
{
    public class XMLDataBase
    {
        private string auditFilePath;
        private string importedFilePath;
        private string loadFilePath;

        public XMLDataBase()
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            this.auditFilePath = Path.Combine(baseDirectory, "Audits.xml");
            this.importedFilePath = Path.Combine(baseDirectory, "ImportedFiles.xml");
            this.loadFilePath = Path.Combine(baseDirectory, "Loads.xml");
        }
        /// <summary>
        /// Prolazi kroz sve audite i updatuje ili ih pravi 
        /// Proverava da li postoji taj xml fajl,ako ne postoji kreira 
        /// I onda preko file stream upisuje,MORA DA SE PRETVORI U LISTU ZATO STO NECE MOCI DA SERIJALIZUJE 
        /// </summary>
        /// <param name="audits"></param>
        public void SaveAudits(Dictionary<int, Audit> audits)
        {
            Dictionary<int, Audit> existingAudits = LoadAudits();

            foreach (Audit audit in audits.Values)
            {
                existingAudits[audit.Id] = audit;
            }

            if (!File.Exists(auditFilePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(auditFilePath));
            }

            using (FileStream fileStream = new FileStream(auditFilePath, FileMode.Create, FileAccess.Write))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<Audit>));
                serializer.Serialize(fileStream, existingAudits.Values.ToList());
            }
        }

        /// <summary>
        /// Isto kao i za ono gore 
        /// </summary>
        /// <param name="importedFiles"></param>
        public void SaveImportedFiles(Dictionary<int, ImportedFile> importedFiles)
        {
            Dictionary<int, ImportedFile> existingImportedFiles = LoadImportedFiles();

            foreach (ImportedFile importedFile in importedFiles.Values)
            {
                existingImportedFiles[importedFile.Id] = importedFile;
            }

            if (!File.Exists(importedFilePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(importedFilePath));
            }

            using (FileStream fileStream = new FileStream(importedFilePath, FileMode.Create, FileAccess.Write))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<ImportedFile>));
                serializer.Serialize(fileStream, existingImportedFiles.Values.ToList());
            }
        }

        /// <summary>
        /// Isto 
        /// </summary>
        /// <param name="loads"></param>
        public void SaveLoads(Dictionary<int, Load> loads)
        {
            Dictionary<int, Load> existingLoads = LoadLoads();

            foreach (Load load in loads.Values)
            {
                existingLoads[load.Id] = load;
            }

            if (!File.Exists(loadFilePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(loadFilePath));
            }

            using (FileStream fileStream = new FileStream(loadFilePath, FileMode.Create, FileAccess.Write))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<Load>));
                serializer.Serialize(fileStream, existingLoads.Values.ToList());
            }
        }
        /// <summary>
        /// Proveri da li postoji xml 
        /// Ako postoji taj xml 
        /// Proverava duzinu da ne radi za dzabe serijalizaciju 
        /// I onda deserijalizuje da bi mogao da vrati dictionary ,pre toga pretvori u dic 
        /// </summary>
        /// <returns></returns>
        public Dictionary<int, Audit> LoadAudits()
        {
            if (File.Exists(auditFilePath))
            {
                long length = new FileInfo(loadFilePath).Length;
                if (length > 0)
                {
                    using (XmlReader reader = XmlReader.Create(auditFilePath))
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(List<Audit>));
                        //Foreach da iz liste audita za svaki objekat prodje i uzme njegov id kao key a value audit 
                        return ((List<Audit>)serializer.Deserialize(reader)).ToDictionary(audit => audit.Id);
                    }
                }
            }
            return new Dictionary<int, Audit>(); // ako ne postji fajl 
        }

        /// <summary>
        /// Isto kao gore 
        /// </summary>
        /// <returns></returns>
        public Dictionary<int, ImportedFile> LoadImportedFiles()
        {
            if (File.Exists(importedFilePath))
            {
                long length = new FileInfo(auditFilePath).Length;
                if (length > 0)
                {
                    using (XmlReader reader = XmlReader.Create(importedFilePath))
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(List<ImportedFile>));
                        return ((List<ImportedFile>)serializer.Deserialize(reader)).ToDictionary(importedFile => importedFile.Id);
                    }
                }
            }
            return new Dictionary<int, ImportedFile>();
        }

        /// <summary>
        /// Isto kao gore 
        /// </summary>
        /// <returns></returns>
        public Dictionary<int, Load> LoadLoads()
        {
            if (File.Exists(loadFilePath))
            {
                long length = new FileInfo(auditFilePath).Length;
                if (length > 0)
                {
                    using (XmlReader reader = XmlReader.Create(loadFilePath))
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(List<Load>));
                        return ((List<Load>)serializer.Deserialize(reader)).ToDictionary(load => load.Id);
                    }
                }
            }
            return new Dictionary<int, Load>();
        }

        /// <summary>
        /// Object source ko je okinuo event 
        /// Eventargs ako se nesto salje 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public void OnListOfFilesProccessed(object source, EventArgs e)
        {
            InMemoryDatabase db = new InMemoryDatabase(); 
            SaveAudits(db.GetAllAudits());
            SaveImportedFiles(db.GetAllImportedFiles());
            SaveLoads(db.GetAllLoads());
        }
    }

}
