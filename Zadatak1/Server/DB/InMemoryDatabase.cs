using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;

namespace Server
{
    public class InMemoryDatabase
    {

        private static readonly ConcurrentDictionary<int, Audit> auditTable = new ConcurrentDictionary<int, Audit>();
        private static readonly ConcurrentDictionary<int, ImportedFile> importedFileTable = new ConcurrentDictionary<int, ImportedFile>();
        private static readonly ConcurrentDictionary<int, Load> loadTable = new ConcurrentDictionary<int, Load>();

        public void SaveLoad(Load load)
        {
            loadTable[load.Id] = load;
        }
        public void SaveAudit(Audit audit)
        {
            auditTable[audit.Id] = audit;
        }

        public void SaveImportedFile(ImportedFile importedFile)
        {
            importedFileTable[importedFile.Id] = importedFile;
        }

        public Load GetLoad(int loadId)
        {
            loadTable.TryGetValue(loadId, out Load load);
            return load;
        }

        public Audit GetAudit(int auditId)
        {
            auditTable.TryGetValue(auditId, out Audit audit);
            return audit;
        }

        public ImportedFile GetImportedFile(int importedFileId)
        {
            importedFileTable.TryGetValue(importedFileId, out ImportedFile importedFile);
            return importedFile;
        }

        public Dictionary<int, Load> GetAllLoads()
        {
            return loadTable.ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        public Dictionary<int, Audit> GetAllAudits()
        {
            return auditTable.ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        public Dictionary<int, ImportedFile> GetAllImportedFiles()
        {
            return importedFileTable.ToDictionary(kv => kv.Key, kv => kv.Value);
        }


        //Fje koje reaguje na event 
        public void OnFileValidated(object source, Audit a)
        {
            SaveAudit(a);
        }

        public void OnRowProccessed(object source, Load l)
        {
            SaveLoad(l);
        }

        public void OnStreamProccessed(object source, ImportedFile iF)
        {
            SaveImportedFile(iF);
        }

    }
}
