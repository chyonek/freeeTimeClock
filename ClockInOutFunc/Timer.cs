using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;
using ClockInOutFunc.Model;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace ClockInOutFunc
{
    public static class Timer
    {
        [FunctionName("Timer")]
        public static async void Run([TimerTrigger("00:01:00")] TimerInfo myTimer, ILogger log)
        {

            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            CloudTable table = GetCloudTable();
            string timestamp = DateTime.UtcNow.ToString();

            //TableOperation retrieveOperation = TableOperation.PartitionScanAsync(table, timestamp);
            //TableResult result = await table.ExecuteAsync(retrieveOperation);
            //CustomEntity customEntity = result.Result as CustomEntity;

            //Console.WriteLine(result);
            //Console.WriteLine(customEntity.PartitionKey + "," + customEntity.RowKey + "," + customEntity.Text);
        }

        private static CloudTable GetCloudTable()
        {
            var storageAccount = CloudStorageAccount.Parse(System.Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference("MyTable");
            return table;
        }


        //public static async Task<CustomEntity> RetrieveEntityUsingPointQueryAsync(CloudTable table, string partitionKey, string rowKey)
        //{
        //    try
        //    {
        //        TableOperation retrieveOperation = TableOperation.Retrieve<CustomEntity>(partitionKey, rowKey);
        //        TableResult result = await table.ExecuteAsync(retrieveOperation);
        //        CustomEntity customEntity = result.Result as CustomEntity;
        //        if (customEntity != null)
        //        {
        //            Console.WriteLine("\t{0}\t{1}\t{2}\t{3}", customEntity.PartitionKey, customEntity.RowKey, customEntity.Text);
        //        }

        //        return customEntity;
        //    }
        //    catch (StorageException e)
        //    {
        //        Console.WriteLine(e.Message);
        //        Console.ReadLine();
        //        throw;
        //    }
        //}


        //private static async Task PartitionScanAsync(CloudTable table, string timestamp)
        //{
        //    try
        //    {
        //        //    TableQuery<CustomEntity> partitionScanQuery =
        //        //new TableQuery<CustomEntity>().Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, rowKey));

        //        var query = new TableQuery<Microsoft.Azure.Cosmos.Table.TableEntity>()
        //            .Where(Microsoft.Azure.Cosmos.Table.TableQuery.GenerateFilterCondition("Timestamp", "eq", partitionKey));
        //        var result = new List<TableEntity>(20000);

        //        TableContinuationToken token = null;

        //        // Read entities from each query segment.
        //        do
        //        {
        //            TableQuerySegment<CustomEntity> segment = await table.ExecuteQuerySegmentedAsync(partitionScanQuery, token);
        //            token = segment.ContinuationToken;
        //            foreach (CustomEntity customEntity in segment)
        //            {
        //                Console.WriteLine("\t{0}\t{1}\t{2}\t{3}", customEntity.PartitionKey, customEntity.RowKey, customEntity.Text);
        //            }
        //        }
        //        while (token != null);
        //    }
        //    catch (StorageException e)
        //    {
        //        Console.WriteLine(e.Message);
        //        Console.ReadLine();
        //        throw;
        //    }
        //}
    }


}
