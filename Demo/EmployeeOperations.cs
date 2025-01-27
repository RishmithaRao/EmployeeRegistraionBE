using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Net;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using Demo.Models;
using Demo.Repository.Implementation;
using Demo.Repository.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json;
using NPOI.OpenXml4Net.OPC;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using System.Configuration;
using System.Linq;
using ServiceStack;
using Demo.Helpers;
using ServiceStack.FluentValidation.Results;
using System.ComponentModel.DataAnnotations;
using Microsoft.Azure.Cosmos.Serialization.HybridRow;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml.Bibliography;

namespace Demo
{

    public static class EmployeeOperations
    {
        [FunctionName(FunctionNamesConstants.GetAllEmployees)]
        [OpenApiOperation(operationId: "GetAllEmployees", tags: new[] { FunctionNamesConstants.GetAllEmployees })]
        public static async Task<IEnumerable<Employee>> GetAllEmployees([HttpTrigger(AuthorizationLevel.Function, HttpVerbsConstants.HttpGet, Route = RouteConstants.GetEmployee)] HttpRequest req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function to get all data from Cosmos DB");

            IDocumentDBRepository<Employee> Respository = new DocumentDBRepository<Employee>();

            var result = await Respository.GetItemsAsync(Constants.TblEmployee);
            return result;
        }

        [FunctionName(FunctionNamesConstants.GetEmployeeById)]
        public static async Task<Employee> GetEmployeeById([HttpTrigger(AuthorizationLevel.Function, HttpVerbsConstants.HttpGet, Route = RouteConstants.GetEmployeeById_id)] HttpRequest req, TraceWriter log, string id)
            {
            log.Info("C# HTTP trigger function to get a single data from Cosmos DB");

            IDocumentDBRepository<Employee> Respository = new DocumentDBRepository<Employee>();
            var employees = await Respository.GetItemsAsync(Constants.TblEmployee);
            employees = employees.Where(X => X.Id == id).ToList();
            Employee employee = new Employee();
            foreach (var emp in employees)
            {
                employee = emp;
                break;
            }
            return employee;
        }

        [FunctionName(FunctionNamesConstants.CreateEmployee)]
        public static async Task<IActionResult> CreateEmployee(
           [HttpTrigger(AuthorizationLevel.Function, HttpVerbsConstants.HttpPost, Route = RouteConstants.Employee_Create)] HttpRequest req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function to create a record into Cosmos DB");
            try
            {
                IDocumentDBRepository<Employee> Respository = new DocumentDBRepository<Employee>();
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var employee = JsonConvert.DeserializeObject<Employee>(requestBody);
                if (employee is null)
                {
                    string responseMessage = "Please fill the details";
                    return new OkObjectResult(responseMessage);
                }
                else
                {
                    employee.Id = null;
                    var validator = new EmployeValidations();
                    var validationResult = validator.Validate(employee);

                    if (!validationResult.IsValid)
                    {
                        return new OkObjectResult(validationResult.Errors.Select(e => new {
                            Field = e.PropertyName,
                            Error = e.ErrorMessage
                        }));
                    }
                }

                var result = await Respository.CreateItemAsync(employee, Constants.TblEmployee);
                return new OkObjectResult(result);
            }
            catch
            {
                log.Info("Error occured while creating a record into Cosmos DB");
                string responseMessage = "Error occured while creating a record into Cosmos DB";
                return new OkObjectResult(responseMessage);
            }
        }

        [FunctionName(FunctionNamesConstants.UpdateEmployee)]
        public static async Task<IActionResult> UpdateEmployee(
            [HttpTrigger(AuthorizationLevel.Function, HttpVerbsConstants.HttpPut, Route = RouteConstants.Employee_Update)] HttpRequest req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function to create a record into Cosmos DB");
            try
            
            {
                IDocumentDBRepository<Employee> Respository = new DocumentDBRepository<Employee>();
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var employee = JsonConvert.DeserializeObject<Employee>(requestBody);
                var validator = new EmployeValidations();
                var validationResult = validator.Validate(employee);

                if (!validationResult.IsValid)
                {
                    return new BadRequestObjectResult(validationResult.Errors.Select(e => new {
                        Field = e.PropertyName,
                        Error = e.ErrorMessage
                    }));
                }
                var result = await Respository.UpdateItemAsync(employee.Id, employee, Constants.TblEmployee);
                return new OkObjectResult(result);
            }
            catch
            {
                log.Info("Error occured while creating a record into Cosmos DB");
                string responseMessage = "Error occured while creating a record into Cosmos DB";
                return new OkObjectResult(responseMessage);
            }
        }

        [FunctionName(FunctionNamesConstants.DeleteEmployee)]
        public static async Task<bool> DeleteEmployee([HttpTrigger(AuthorizationLevel.Function, HttpVerbsConstants.HttpDelete, Route = RouteConstants.Employee_Delete_id)] HttpRequest req, TraceWriter log, string id)
        {
            log.Info("C# HTTP trigger function to delete a record from Cosmos DB");

            IDocumentDBRepository<Employee> Respository = new DocumentDBRepository<Employee>();
            try
            {
                await Respository.DeleteItemAsync(id, Constants.TblEmployee, id);
                return true;
            }
            catch
            {
                return false;
            }
        }

        [FunctionName(FunctionNamesConstants.GetEmployeesPagination)]

        public static async Task<IActionResult> GetEmployeesPagination([HttpTrigger(AuthorizationLevel.Function,
    HttpVerbsConstants.HttpGet, Route = RouteConstants.GetEmployeesPagination)] HttpRequest req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function to get all data from Cosmos DB");
            string pageSizeParam = req.Query["pageSize"];
            string pageNumberParam = req.Query["pageNumber"];
            string searchTerm = req.Query["searchTerm"];
            int pageSize = int.TryParse(pageSizeParam, out int parsedPageSize) ? parsedPageSize : 10; // Default page size: 10
            int pageNumber = int.TryParse(pageNumberParam, out int parsedPageNumber) ? parsedPageNumber : 1; // Default page number: 1

            IDocumentDBRepository<Employee> Respository = new DocumentDBRepository<Employee>();
            var (totalEmployees, employees) = await Respository.GetItemsAsync(Constants.TblEmployee, pageSize, pageNumber, searchTerm);
            return new JsonResult(new
            {
                totalEmployees,
                employees = employees
            });
        }
    }
}

