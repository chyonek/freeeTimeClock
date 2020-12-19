using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Cosmos.Table;
using ClockInOutFunc.Model;
using System.Diagnostics;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;

namespace ClockInOutFunc
{
    public static class Function1
    {
        private static HttpClient client = new HttpClient();

        [FunctionName("TableOutput")]
        //[return: Table("MyTable")]
        public static async Task<bool> Run([HttpTrigger] HttpRequest req, ILogger log)
        {
            Task<Me> meInfo;
            string date = DateTime.Now.ToString("yyyyMMdd");
            string lastHeatbeatTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:dd");

            // リクエストを送ったユーザーを特定
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            log.LogInformation($"C# http trigger function processed: {requestBody}");
            string accessToken = JsonConvert.DeserializeObject<RequestJson>(requestBody).AccessToken;

            meInfo = GetMeInfo(accessToken);
            meInfo.Wait();
            var me = meInfo.Result;

            // 既存のエンティティがあるかを確認 (ある場合 = 出勤済なので accessToken, LastHeatbeatTime を更新、ない場合 = 未出勤なので Entity 作成)
            // ★所属している事業所が一つしかない場合、Companies[1] は Companies[0] にします
            CloudTable table = GetCloudTable();
            CustomEntity customEntity = new CustomEntity { PartitionKey = me.Companies[1].EmployeeId, RowKey = date, Text = accessToken , LastHeatbeatTime = lastHeatbeatTime };
            await InsertOrMergeEntityAsync(table, customEntity);

            // 出勤できるかどうか確認。出勤できるなら出勤する。できないなら何もしない。
            string availableTypes = await CheckAvailableTypes(accessToken, me.Companies[1].CompanyId, me.Companies[1].EmployeeId);

            if (availableTypes.Contains("clock_in"))
            {
                await ClockIn(accessToken, me.Companies[1].CompanyId, me.Companies[1].EmployeeId, lastHeatbeatTime);
                log.LogInformation($"出勤");
            }
            else
            { 
                log.LogInformation($"何もしない");
            }
            return true;
        }


        // ログインユーザーの情報を取得
        private static async Task<Me> GetMeInfo(string accessToken)
        {
            Debug.WriteLine("GetMeInfo Start");
            Me result = null;

            try
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var task = client.GetAsync("https://api.freee.co.jp/hr/api/v1/users/me");
                task.Wait();
                var response = task.Result;

                var jsonData = await response.Content.ReadAsStringAsync();
                Debug.WriteLine(jsonData);
                result = JsonConvert.DeserializeObject<Me>(jsonData);

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            return result;
        }

        // 打刻可能種別の取得 GET /api/v1/employees/{emp_id}/time_clocks/available_types
        private static async Task<string> CheckAvailableTypes(string accessToken, int companyId, string empId)
        {
            Debug.WriteLine("CheckAvailableTypes Start");
            string result;

            try
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var task = client.GetAsync($"https://api.freee.co.jp/hr/api/v1/employees/{empId}/time_clocks/available_types?company_id={companyId}");
                task.Wait();
                var response = task.Result;

                result = await response.Content.ReadAsStringAsync();
                //Debug.WriteLine(jsonData);
                //result = JsonConvert.DeserializeObject<AvailableTypes>(jsonData).AvailableTypesString;

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                result = ex.Message;
            }

            return result;
        }


        // 出勤打刻、POST /api/v1/{empId}/time_clocks
        private static async Task<bool> ClockIn(string accessToken, int companyId, string empId, string datetime)
        {
            bool result;
            Debug.WriteLine("ClockIn() Start");

            try
            {
                var form = new Dictionary<string, object>
                {
                    {"company_id", companyId},
                    {"type", "clock_in"},
                    //{"base_date", DateTime.Today.ToString("yyyy-MM-dd")},
                    {"datetime", datetime }
                };
                var json = JsonConvert.SerializeObject(form);
                var content = new StringContent(json, Encoding.UTF8, @"application/json");

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var task = client.PostAsync($"https://api.freee.co.jp/hr/api/v1/employees/{empId}/time_clocks", content);
                task.Wait();
                var response = task.Result;

                var jsonData = await response.Content.ReadAsStringAsync();
                Debug.WriteLine(jsonData);
                result = true;

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                result = false;
            }

            return result;
        }

        // Storage Util
        private static CloudTable GetCloudTable()
        {
            var storageAccount = CloudStorageAccount.Parse(System.Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference("MyTable");
            return table;
        }
        /// <summary>
        /// The Table Service supports two main types of insert operations.
        ///  1. Insert - insert a new entity. If an entity already exists with the same PK + RK an exception will be thrown.
        ///  2. Replace - replace an existing entity. Replace an existing entity with a new entity.
        ///  3. Insert or Replace - insert the entity if the entity does not exist, or if the entity exists, replace the existing one.
        ///  4. Insert or Merge - insert the entity if the entity does not exist or, if the entity exists, merges the provided entity properties with the already existing ones.
        /// </summary>
        /// <param name="table">The sample table name</param>
        /// <param name="entity">The entity to insert or merge</param>
        /// <returns>A Task object</returns>
        public static async Task<CustomEntity> InsertOrMergeEntityAsync(CloudTable table, CustomEntity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }

            try
            {
                // Create the InsertOrReplace table operation
                TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(entity);

                // Execute the operation.
                TableResult result = await table.ExecuteAsync(insertOrMergeOperation);
                CustomEntity insertedcustomEntity = result.Result as CustomEntity;

                return insertedcustomEntity;
            }
            catch (StorageException e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
                throw;
            }
        }
    }


    internal class RequestJson
    {
        [JsonProperty("accessToken")]
        public string AccessToken { get; set; }

    }

    internal class AvailableTypes
    {
        [JsonProperty("available_types")]
        public string AvailableTypesString { get; set; }

    }

    internal class Me
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        public List<Company> Companies { get; set; }

    }

    internal class Company
    {
        [JsonProperty("id")]
        public int CompanyId { get; set; }

        [JsonProperty("name")]
        public string CompanyName { get; set; }

        [JsonProperty("employee_id")]
        public string EmployeeId { get; set; }

    }


}
