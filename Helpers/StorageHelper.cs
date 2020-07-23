using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage.Table;


namespace CoreBot.Helpers
{
    public class StorageHelper
    {
        //private static IConfiguration _configuration;
        private static CloudStorageAccount _storageAccount;
        private static CloudTableClient _tableClient;
        private static CloudTable _feedbackTable;



        public async Task StoreFeedback(IConfiguration _configuration, FeedbackEntity feedbackEntity)
        {
            var connectionString = _configuration["storageConnectionString"];
            var tableName = "feedback";
            TableOperation insertOperation = TableOperation.InsertOrMerge(feedbackEntity);

            try
            {
                _storageAccount = CloudStorageAccount.Parse(connectionString);

                // Create the table client.
                _tableClient = _storageAccount.CreateCloudTableClient();

                // Get a reference to the table
                _feedbackTable = _tableClient.GetTableReference(tableName);
                await _feedbackTable.ExecuteAsync(insertOperation);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Insert to table failed with error {e.Message}");
                throw;
            }
        }

        public async Task<string> GetRole(IConfiguration _configuration, string partitionKey, string rowKey)
        {
            FeedbackEntity feedbackEntity = new FeedbackEntity();
            var connectionString = _configuration["storageConnectionString"];
            var tableName = "users";
            TableOperation retrieveOperation = TableOperation.Retrieve<FeedbackEntity>(partitionKey, rowKey);

            try
            {
                _storageAccount = CloudStorageAccount.Parse(connectionString);
                // Create the table client.
                _tableClient = _storageAccount.CreateCloudTableClient();
                // Get a reference to the table
                _feedbackTable = _tableClient.GetTableReference(tableName);
                TableResult query = await _feedbackTable.ExecuteAsync(retrieveOperation);
                if (query.Result != null)
                    return ((FeedbackEntity)query.Result).Role;
                else
                    return string.Empty;

            }
            catch (Exception e)
            {
                Console.WriteLine($"Insert to table failed with error {e.Message}");
                throw;
            }
        }

        internal async Task<string> getActions(IConfiguration configuration, string partitionKey, string sAction)
        {
            RoleAction feedbackEntity = new RoleAction();
            var connectionString = configuration["storageConnectionString"];
            var tableName = "roleaction";
            TableOperation retrieveOperation = TableOperation.Retrieve<RoleAction>(partitionKey, partitionKey);

            try
            {
                _storageAccount = CloudStorageAccount.Parse(connectionString);
                // Create the table client.
                _tableClient = _storageAccount.CreateCloudTableClient();
                // Get a reference to the table
                _feedbackTable = _tableClient.GetTableReference(tableName);
                TableResult query = await _feedbackTable.ExecuteAsync(retrieveOperation);
                if (query.Result != null)
                  if(sAction.Equals("Action")) return ((RoleAction)query.Result).Action; else return ((RoleAction)query.Result).SubAction;
                else
                    return string.Empty;

            }
            catch (Exception e)
            {
                Console.WriteLine($"Insert to table failed with error {e.Message}");
                throw;
            }
        }
    }
}
