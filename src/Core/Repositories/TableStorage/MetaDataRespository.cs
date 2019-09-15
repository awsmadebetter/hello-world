﻿using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Bit.Core.Models.Data;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace Bit.Core.Repositories.TableStorage
{
    public class MetaDataRespository : IMetaDataRespository
    {
        private readonly CloudTable _table;

        public MetaDataRespository(GlobalSettings globalSettings)
            : this(globalSettings.Events.ConnectionString)
        { }

        public MetaDataRespository(string storageConnectionString)
        {
            var storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            _table = tableClient.GetTableReference("metadata");
        }

        public async Task<IDictionary<string, string>> GetAsync(string id)
        {
            var query = new TableQuery<DictionaryEntity>().Where(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, id));
            var queryResults = await _table.ExecuteQuerySegmentedAsync(query, null);
            return queryResults.Results.FirstOrDefault()?.ToDictionary(d => d.Key, d => d.Value.StringValue);
        }

        public async Task<string> GetAsync(string id, string prop)
        {
            var dict = await GetAsync(id);
            if(dict != null && dict.ContainsKey(prop))
            {
                return dict[prop];
            }
            return null;
        }

        public async Task UpsertAsync(string id, KeyValuePair<string, string> keyValuePair)
        {
            var query = new TableQuery<DictionaryEntity>().Where(
                   TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, id));
            var queryResults = await _table.ExecuteQuerySegmentedAsync(query, null);
            var entity = queryResults.Results.FirstOrDefault();
            if(entity == null)
            {
                entity = new DictionaryEntity
                {
                    PartitionKey = id
                };
            }
            if(entity.ContainsKey(keyValuePair.Key))
            {
                entity.Remove(keyValuePair.Key);
            }
            entity.Add(keyValuePair.Key, keyValuePair.Value);
            await _table.ExecuteAsync(TableOperation.InsertOrReplace(entity));
        }

        public async Task UpsertAsync(string id, IDictionary<string, string> dict)
        {
            var entity = new DictionaryEntity
            {
                PartitionKey = id
            };
            foreach(var item in dict)
            {
                entity.Add(item.Key, item.Value);
            }
            await _table.ExecuteAsync(TableOperation.InsertOrReplace(entity));
        }

        public async Task DeleteAsync(string id)
        {
            try
            {
                await _table.ExecuteAsync(TableOperation.Delete(new DictionaryEntity
                {
                    PartitionKey = id,
                    ETag = "*"
                }));
            }
            catch(StorageException e)
            {
                if(e.RequestInformation.HttpStatusCode != (int)HttpStatusCode.NotFound)
                {
                    throw e;
                }
            }
        }
    }
}
