using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace MyNewService
{
    static class Program
    {
        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        static void Main(string[] args)
        {
            // for Debug
            if (Environment.UserInteractive)
            {
                freeeTimeClockService service1 = new freeeTimeClockService(args);
                service1.TestStartupAndStop(args);
            } 
            // for Start as Windows Servivce
            else
            { 
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                    new freeeTimeClockService(args)
                };
                ServiceBase.Run(ServicesToRun);
            }
        }
    }
}
