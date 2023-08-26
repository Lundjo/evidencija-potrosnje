using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Client.Path;
using System.IO;
using Client.FileSending;
using Microsoft.Win32;
using System.Windows.Forms;
using Common;

namespace Client
{

    public partial class MainWindow : Window
    {
        private ChannelFactory<IFileHandling> factory;
        private static IFileHandling proxy; //preko ovoga komuniciramo sa serverom 
        private static IFileSender fileSender; 
        public MainWindow()
        {

            InitializeComponent();
            factory = new ChannelFactory<IFileHandling>("SendGetFile");
            proxy = factory.CreateChannel();
            fileSender = new FileSender(proxy); // preko ovoga se pripremaju fajlovi za slanje 
                                                // kreira se memory stream i pakuju se u strukturu FileOverNetwork
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            //string path = Path.Path.DirPath; // izvlacim dir path 
            if (Path.Path.FilePaths.Length != 0)
            {
                fileSender.SendFiles(Path.Path.FilePaths); // samo uzme fajlove sa odredjene putanje i posalje ih 
            }
        }
        /// <summary>
        /// Path i da onda on krene 
        /// Uploader da ima u startu string koji je path 
        /// </summary>
        /// <param name="proxy"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            Path.Path.fileNames.Clear();// interna struktura za cuvanje imena fajlova 
            allFiles.Items.Clear(); // ovo je lista iz WPF-a 
            FolderBrowserDialog openFolder = new FolderBrowserDialog();
            openFolder.ShowDialog();
            if (openFolder.SelectedPath == null || openFolder.SelectedPath=="")
            {
                openFolder.Dispose(); // provera da li je nesto selektovano da se pokupi sa te putanje ako nije zatvara se prozor 
                return;
            }
            GetFileNames(openFolder.SelectedPath);
            Path.Path.selectedPath = openFolder.SelectedPath;
            AddFileNames(Path.Path.fileNames);
            openFolder.Dispose();
        }

        /// <summary>
        /// Get all file names to display 
        /// </summary>
        /// <param name="dirPath"></param>
        private void GetFileNames(string dirPath)
        {
            string[] files = SearchDirectory(dirPath);
            foreach (string filePath in files)
            {
                // c://dusan/kurac/kita/forecast.csv 
                // ova fja odseca sve sem forecast.csv 
                string[] curFileName = filePath.Split('\\');
                Path.Path.fileNames.Add(curFileName[curFileName.Length-1]);
            }
            Path.Path.FilePaths = files; // putanja do svih fajlova koji se izvlace prilikom jednog importa 
        }

        /// <summary>
        /// Gleda sve child dir u tom selektovanom folderu koji se zovu MEASURED i FORECAST i onda kupi sve fajlove iz njih koji pocinju sa forecast ili measured 
        /// </summary>
        /// <param name="dirPath"></param>
        /// <returns></returns>
        private string[] SearchDirectory(string dirPath)
        {
            DirectoryInfo di = new DirectoryInfo(dirPath);
            List<string> paths = new List<string>();
            foreach(DirectoryInfo directory in di.GetDirectories())
            {
                if(directory.Name.ToUpper()=="MEASURED" || directory.Name.ToUpper() == "FORECAST")
                {
                    string[] filesInFolder = Directory.GetFiles(directory.FullName, "*.csv", SearchOption.TopDirectoryOnly);
                    foreach(string filePath in filesInFolder)
                    {
                        string[] curFileName = filePath.Split('\\');
                        string fileName = curFileName[curFileName.Length - 1];
                        if(fileName.ToUpper().StartsWith("FORECAST") || fileName.ToUpper().StartsWith("MEASURED"))
                        {
                            paths.Add(filePath);
                        }
                    }
                }
            }
            string[] pathsArray = paths.ToArray<string>(); // iz nekog razloga nije htelo da radi sa arrayom pa smo prvo pravili listu 
                                                           // pa prebacili u array i to smo vracali 
            paths.Clear();
            return pathsArray;
        }
        /// <summary>
        /// Dodaje samo ona splitovana imena fajlova u tu listu koju ce prikazivati na UI 
        /// </summary>
        /// <param name="fileNames"></param>
        private void AddFileNames(List<string>fileNames)
        {
            if(fileNames.Count > 0)
            {
                foreach (string name in fileNames)
                {
                    allFiles.Items.Add(name); // list view se zove allFiles 
                }
            }
        }
    }
}
