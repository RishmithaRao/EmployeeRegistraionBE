using Demo.Repository.Interface;
using DocumentFormat.OpenXml.Office.CustomUI;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using NPOI.SS.Formula.Eval;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Office2010.Excel;
using Demo.Models;

namespace Demo.Repository.Implementation
{
    public class DocumentDBRepository<T> : IDocumentDBRepository<T> where T : Employee
    {
        private readonly string Endpoint = "https://localhost:8081";
        private readonly string Key = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
        private readonly string DatabaseId = "Employee1";
        private DocumentClient client;

        public DocumentDBRepository()
        {
            client = new DocumentClient(new Uri(Endpoint), Key);
        }               

        public async Task<IEnumerable<T>> GetItemsAsync(string collectionId)
        {
            IDocumentQuery<T> query = client.CreateDocumentQuery<T>(
                UriFactory.CreateDocumentCollectionUri(DatabaseId, collectionId),
                new FeedOptions { MaxItemCount = -1 })
                .AsDocumentQuery();


            List<T> results = new List<T>();
            while (query.HasMoreResults)
            {
                results.AddRange(await query.ExecuteNextAsync<T>());
            }

            return results;
        }
        //public async Task<IEnumerable<Employee>> GetItemsAsync( string collectionId,int pageSize, int pageNumber,string searchTerm = "")
        //{
        //    var query = client.CreateDocumentQuery<Employee>(
        //UriFactory.CreateDocumentCollectionUri(DatabaseId, collectionId),
        //new FeedOptions { MaxItemCount = pageSize }) // Set the max items per query
        //.AsQueryable();

        //    // Ensure the search term is not null
        //    searchTerm = searchTerm ?? "";


        //    // Apply filtering if a search term is provided
        //    if (!string.IsNullOrWhiteSpace(searchTerm))
        //    {
        //        query = query.Where(e =>
        //            e.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) || // Search in Name
        //            e.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) || // Search in Email
        //            e.Cityname.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) || // Search in Cityname
        //            e.Designation.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)); // Search in Designation
        //    }

        //    // Apply pagination (skip and take)
        //    return query
        //        .Skip(pageSize * (pageNumber - 1)) // Skip items for previous pages
        //        .Take(pageSize)                   // Take items for the current page
        //        .ToList();
        //}
        //public async Task<IEnumerable<Employee>> GetItemsAsync(string collectionId, int pageSize, int pageNumber, string searchTerm = "")
        //{
        //    var query = client.CreateDocumentQuery<Employee>(
        //        UriFactory.CreateDocumentCollectionUri(DatabaseId, collectionId),
        //        new FeedOptions { MaxItemCount = pageSize })
        //        .AsEnumerable();

        //    // Ensure the search term is not null
        //    searchTerm = searchTerm ?? "";

        //    if (!string.IsNullOrWhiteSpace(searchTerm))
        //    {
        //        query = query.Where(e =>
        //            e.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
        //            e.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
        //            e.Cityname.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
        //            e.Designation.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
        //    }

        //    var employees = query
        //        .Skip(pageSize * (pageNumber - 1))
        //        .Take(pageSize)
        //        .ToList();

        //    return employees; // Return just the employees
        //}

        // You might want to implement another method to get the total count
        //public async Task<int> GetTotalCountAsync(string collectionId, string searchTerm = "")
        //{
        //    var query = client.CreateDocumentQuery<Employee>(
        //        UriFactory.CreateDocumentCollectionUri(DatabaseId, collectionId),
        //        new FeedOptions { MaxItemCount = -1 }) // Set to -1 to get all without pagination
        //        .AsEnumerable();

        //    searchTerm = searchTerm ?? "";

        //    if (!string.IsNullOrWhiteSpace(searchTerm))
        //    {
        //        query = query.Where(e =>
        //            e.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
        //            e.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
        //            e.Cityname.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
        //            e.Designation.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
        //    }

        //    return query.Count(); // Get the total count
        //}

        public async Task<(int totalCount, IEnumerable<T> employees)> GetItemsAsync(string collectionId, int pageSize, int pageNumber, string searchTerm = "")
        {
            // Assume `client` is your document client for Cosmos DB
            IDocumentQuery<T> query = client.CreateDocumentQuery<T>(
                UriFactory.CreateDocumentCollectionUri(DatabaseId, collectionId),
                new FeedOptions { MaxItemCount = pageSize })
                .AsDocumentQuery();

            List<T> allResults = new List<T>();

            while (query.HasMoreResults)
            {
                var results = await query.ExecuteNextAsync<T>();
                allResults.AddRange(results);
            }

            // Get total count of employees (before filtering and pagination)
            int totalCount = allResults.Count;

            // Filter by search term if provided
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                allResults = allResults.Where(e => e.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Implement pagination
            var paginatedResults = allResults.Skip((pageNumber - 1) * pageSize).Take(pageSize);

            return (totalCount, paginatedResults);
        }



        public async Task<Microsoft.Azure.Documents.Document> CreateItemAsync(T item, string collectionId)
        {
            return await client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(DatabaseId, collectionId), item);
        }

        public async Task<Microsoft.Azure.Documents.Document> UpdateItemAsync(string id, T item, string collectionId)
        {
            return await client.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(DatabaseId, collectionId, id), item);
        }

        public async Task DeleteItemAsync(string id, string collectionId, string partitionKey)
        {
           

            try
            {
                var documentUri = UriFactory.CreateDocumentUri(DatabaseId, collectionId, id);
                var requestOptions = new RequestOptions { PartitionKey = new PartitionKey(id) };
                var response = await client.DeleteDocumentAsync(documentUri, requestOptions);
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

      

    }
}

