using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using System.Configuration;

namespace CloudProvisioningWebJob
{
    // To learn more about Microsoft Azure WebJobs SDK, please see http://go.microsoft.com/fwlink/?LinkID=320976
    class Program
    {

        #region Program input
        private static string user;
        private static string password;
        private static string domain;
        private static string clientId;
        private static string clientSecret;

        #region Variables
        public static string User
        {
            get
            {
                if (String.IsNullOrEmpty(user))
                {
                    user = ConfigurationManager.AppSettings["user"];
                }
                if (String.IsNullOrEmpty(user))
                {
                    user = GetInput("User", false);
                }
                return user;
            }
        }

        public static string Password
        {
            get
            {
                if (String.IsNullOrEmpty(password))
                {
                    password = ConfigurationManager.AppSettings["password"];
                }
                if (String.IsNullOrEmpty(password))
                {
                    password = GetInput("Password", true);
                }
                return password;
            }
        }

        public static string Domain
        {
            get
            {
                if (String.IsNullOrEmpty(domain))
                {
                    domain = ConfigurationManager.AppSettings["domain"];
                }
                if (String.IsNullOrEmpty(domain))
                {
                    domain = GetInput("Domain", false);
                }
                return domain;
            }
        }

        public static string ClientId
        {
            get
            {
                if (String.IsNullOrEmpty(clientId))
                {
                    clientId = ConfigurationManager.AppSettings["clientid"];
                }
                if (String.IsNullOrEmpty(clientId))
                {
                    clientId = GetInput("ClientId", false);
                }
                return clientId;
            }
        }

        public static string ClientSecret
        {
            get
            {
                if (String.IsNullOrEmpty(clientSecret))
                {
                    clientSecret = ConfigurationManager.AppSettings["clientsecret"];
                }
                if (String.IsNullOrEmpty(clientSecret))
                {
                    clientSecret = GetInput("ClientSecret", true);
                }
                return clientSecret;
            }
        }
        #endregion


        #region Helpers
        /// <summary>
        /// Gets the password input from the console window
        /// </summary>
        /// <returns>the entered string</returns>
        private static string GetInput(string label, bool isPassword)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("{0} : ", label);
            Console.ForegroundColor = ConsoleColor.Gray;

            string strPwd = "";

            for (ConsoleKeyInfo keyInfo = Console.ReadKey(true); keyInfo.Key != ConsoleKey.Enter; keyInfo = Console.ReadKey(true))
            {
                if (keyInfo.Key == ConsoleKey.Backspace)
                {
                    if (strPwd.Length > 0)
                    {
                        strPwd = strPwd.Remove(strPwd.Length - 1);
                        Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                        Console.Write(" ");
                        Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                    }
                }
                else if (keyInfo.Key != ConsoleKey.Enter)
                {
                    if (isPassword)
                    {
                        Console.Write("*");
                    }
                    else
                    {
                        Console.Write(keyInfo.KeyChar);
                    }
                    strPwd += keyInfo.KeyChar;

                }

            }
            Console.WriteLine("");

            return strPwd;
        }
        #endregion



        // Please set the following connection strings in app.config for this WebJob to run:
        // AzureWebJobsDashboard and AzureWebJobsStorage
        static void Main()
        {
            //var host = new JobHost();
            //// The following code will invoke a function called ManualTrigger and 
            //// pass in data (value in this case) to the function
            //host.Call(typeof(Functions).GetMethod("ManualTrigger"), new { value = 20 });

            SiteProvisioningTimerJob job = new SiteProvisioningTimerJob();
            Console.WriteLine("Using the following strings: ClientId:" 
                + ConfigurationManager.AppSettings["ClientId"].ToString() + "; ClientSecret: " 
                + ConfigurationManager.AppSettings["ClientSecret"] + "; Site: " 
                + ConfigurationManager.AppSettings["SiteUrl"]);


            job.UseAppOnlyAuthentication(ClientId, ClientSecret);
            

            job.AddSite(ConfigurationManager.AppSettings["SiteUrl"].ToString());
            Console.WriteLine("Running job...");
            job.Run();
        }

        #endregion
    }
}
