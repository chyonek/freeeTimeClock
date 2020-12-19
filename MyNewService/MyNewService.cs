using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Net.Http;
using System.Threading;
using System.Timers;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Configuration;

namespace MyNewService
{
    public partial class freeeTimeClockService : ServiceBase
    {
        static readonly HttpClient client = new HttpClient();
        static readonly HttpClient getTokenClient = new HttpClient();


        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(System.IntPtr handle, ref ServiceStatus serviceStatus);

        public enum ServiceState
        {
            SERVICE_STOPPED = 0x00000001,
            SERVICE_START_PENDING = 0x00000002,
            SERVICE_STOP_PENDING = 0x00000003,
            SERVICE_RUNNING = 0x00000004,
            SERVICE_CONTINUE_PENDING = 0x00000005,
            SERVICE_PAUSE_PENDING = 0x00000006,
            SERVICE_PAUSED = 0x00000007,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ServiceStatus
        {
            public int dwServiceType;
            public ServiceState dwCurrentState;
            public int dwControlsAccepted;
            public int dwWin32ExitCode;
            public int dwServiceSpecificExitCode;
            public int dwCheckPoint;
            public int dwWaitHint;
        };


        public freeeTimeClockService(string[] args)
        {
            InitializeComponent();
            Debug.WriteLine("freeeTimeClockService");

            client.BaseAddress = new Uri("http://localhost:7071/api/");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        protected override void OnStart(string[] args)
        {
            // Update the service state to Start Pending.
            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_START_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            // Set up a timer that triggers every minute.
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Interval = 60000; // 60 seconds
            timer.Elapsed += new ElapsedEventHandler(this.OnTimerAsync);
            timer.Start();

            // Update the service state to Running.
            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
        }

        protected override void OnStop()
        {

        }

        protected override void OnShutdown()
        {
        }

        public async void OnTimerAsync(object sender, ElapsedEventArgs args)
        {
            Debug.WriteLine("In OnTimer");

            string accessToken = "";

            try
            {
                Token getTokenResult = GetToken().Result;
                if (!String.IsNullOrEmpty(getTokenResult.Error))
                {
                    Debug.WriteLine(getTokenResult.Error + ": " + getTokenResult.Error_Description);
                }
                accessToken = getTokenResult.AccessToken;
                Debug.WriteLine(accessToken);

                string urlParameter = "TableOutput";
                string datetime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var form = new Dictionary<string, object>
                {
                    {"accessToken", accessToken }
                };
                var json = JsonConvert.SerializeObject(form);
                var content = new StringContent(json, Encoding.UTF8, @"application/json");

                HttpResponseMessage response = await client.PostAsync(urlParameter, content);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                Debug.WriteLine(responseBody);
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
            }

        }

        protected override void OnContinue()
        {
            Debug.WriteLine("In OnContinue");
        }

        // for Debug
        internal void TestStartupAndStop(string[] args)
        {
            this.OnStart(args);
            Console.ReadLine();
            this.OnStop();
        }

        // API トークン取得
        private async Task<Token> GetToken()
        {
            string baseAddress = @"https://accounts.secure.freee.co.jp/public_api/token";

            string redirect_uri = "urn:ietf:wg:oauth:2.0:oob";
            string client_id = ConfigurationManager.AppSettings["client_id"];
            string client_secret = ConfigurationManager.AppSettings["client_secret"];
            string code = ConfigurationManager.AppSettings["code"];
            string refresh_token = Environment.GetEnvironmentVariable("freeeApiRefreshToken", EnvironmentVariableTarget.Machine);

            Dictionary<string, string> form = null;

            if (!String.IsNullOrEmpty(refresh_token))
            {
                form = new Dictionary<string, string>
                    {
                        {"grant_type", "refresh_token"},
                        {"redirect_uri", redirect_uri},
                        {"client_id", client_id},
                        {"client_secret", client_secret},
                        {"refresh_token", refresh_token},
                    };
            }
            else
            {
                form = new Dictionary<string, string>
                    {
                        {"grant_type", "authorization_code"},
                        {"redirect_uri", redirect_uri},
                        {"client_id", client_id},
                        {"client_secret", client_secret},
                        {"code", code},
                    };
            }

            getTokenClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            getTokenClient.DefaultRequestHeaders.Add("cache-control", "no-cache");
            HttpResponseMessage tokenResponse = await getTokenClient.PostAsync(baseAddress, new FormUrlEncodedContent(form));
            var jsonContent = await tokenResponse.Content.ReadAsStringAsync();
            Token tok = JsonConvert.DeserializeObject<Token>(jsonContent);

            Environment.SetEnvironmentVariable("freeeApiRefreshToken", tok.RefreshToken, EnvironmentVariableTarget.Machine);

            Debug.WriteLine(tok);

            return tok;
        }
    }



    internal class Token
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonProperty("error")]
        public string Error { get; set; }

        [JsonProperty("error_description")]
        public string Error_Description { get; set; }
    }

}
