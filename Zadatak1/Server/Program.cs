using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    internal class Program
    {
        static void Main(string[] args)
        {
            using (ServiceHost host = new ServiceHost(typeof(FileHandlingServer)))
            {
                host.Open();
                Console.WriteLine("Service is open,waiting for files");
                Console.ReadKey();

            }
        }
    }
}
