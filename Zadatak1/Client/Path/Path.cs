using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Client.Path
{
    public class Path
    {
        public static List<string> fileNames = new List<string>(); // lista za prikaz na wpf
        public static string[] FilePaths = new string[0]; // putanja sa koje su pokupljeni 
        public static string selectedPath; // ovo nam je trebalo zato da bi znali u koji folder se pravi results 
    }
}
