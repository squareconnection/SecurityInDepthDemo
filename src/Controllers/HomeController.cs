using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SecurityInDepthDemo.Models;
using SecurityInDepthDemo.Services;

namespace SecurityInDepthDemo.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private ICosmosDbService _cosmosDbService;
        private readonly IConfiguration _configuration;

        //static db info such as name etc
        string databaseName = "Tasks";
        string containerName = "Items";

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        private async Task<string> GetKeyFromKeyVault() {
                var kv = new SecretClient(new Uri("https://completecloudguruvault.vault.azure.net/"), new DefaultAzureCredential());
                var secret = await kv.GetSecretAsync("CosmosDbConnectionKey");
                return secret.Value.Value;
        }

        private async Task<ICosmosDbService> GetCosmosDbServiceWithStaticInfo() {
            
            string account = _configuration.GetValue<string>("CosmosDb:SimpleAccount");
            string key = _configuration.GetValue<string>("CosmosDb:SimpleKey");
            Microsoft.Azure.Cosmos.CosmosClient client = new Microsoft.Azure.Cosmos.CosmosClient(account, key);
            CosmosDbService cosmosDbService = new CosmosDbService(client, databaseName, containerName);
            Microsoft.Azure.Cosmos.DatabaseResponse database = await client.CreateDatabaseIfNotExistsAsync(databaseName);
            await database.Database.CreateContainerIfNotExistsAsync(containerName, "/id");
            return cosmosDbService;
        }

        private async Task<ICosmosDbService> GetNetworkProtectedCosmosDbServiceWithStaticInfo()
        {

            string account = _configuration.GetValue<string>("CosmosDb:NetworkProtectedAccount");
            string key = _configuration.GetValue<string>("CosmosDb:NetworkProtectedKey");
            Microsoft.Azure.Cosmos.CosmosClient client = new Microsoft.Azure.Cosmos.CosmosClient(account, key);
            CosmosDbService cosmosDbService = new CosmosDbService(client, databaseName, containerName);
            Microsoft.Azure.Cosmos.DatabaseResponse database = await client.CreateDatabaseIfNotExistsAsync(databaseName);
            await database.Database.CreateContainerIfNotExistsAsync(containerName, "/id");
            return cosmosDbService;
        }

        private async Task<ICosmosDbService> GetCosmosDbServiceUsingKeyVault()
        {

            string account = _configuration.GetValue<string>("CosmosDb:SimpleAccount");

            //Get the CosmosDb key from a KeyVault/
            string key = await GetKeyFromKeyVault();


            Microsoft.Azure.Cosmos.CosmosClient client = new Microsoft.Azure.Cosmos.CosmosClient(account, key);
            CosmosDbService cosmosDbService = new CosmosDbService(client, databaseName, containerName);
            Microsoft.Azure.Cosmos.DatabaseResponse database = await client.CreateDatabaseIfNotExistsAsync(databaseName);
            await database.Database.CreateContainerIfNotExistsAsync(containerName, "/id");
            return cosmosDbService;
        }

        private async Task<ICosmosDbService> GetCosmosDbServiceUsingManagedIdentity()
        {

            string account = _configuration.GetValue<string>("CosmosDb:SimpleAccount");

            Microsoft.Azure.Cosmos.CosmosClient client = new Microsoft.Azure.Cosmos.CosmosClient(account, new DefaultAzureCredential());
            CosmosDbService cosmosDbService = new CosmosDbService(client, databaseName, containerName);
            Microsoft.Azure.Cosmos.DatabaseResponse database = await client.CreateDatabaseIfNotExistsAsync(databaseName);
            await database.Database.CreateContainerIfNotExistsAsync(containerName, "/id");
            return cosmosDbService;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.Message = "Click a button to get started!";
            return View(null);
        }

        public async Task<IActionResult> SimpleConnection()
        {
            ViewBag.Message = "Got some data just using a connection string with a secret key.";

            try
            {
                //Create CosmosDb connection
                _cosmosDbService = await GetCosmosDbServiceWithStaticInfo();

            var results = (await _cosmosDbService.GetItemsAsync("SELECT * FROM c"));
            return View("Index", results);
            }
            catch (Exception ex)
            {
                ViewBag.Message = ex.Message;
            }
            return View("Index");
        }

        public async Task<IActionResult> NetworkProtectedConnection()
        {
            ViewBag.Message = "Got some data just using a protected network and a connection string with a secret key fron config.";

            try
            {
                //Create CosmosDb connection
                _cosmosDbService = await GetNetworkProtectedCosmosDbServiceWithStaticInfo();

                var results = (await _cosmosDbService.GetItemsAsync("SELECT * FROM c"));
                return View("Index", results);
            }
            catch (Exception ex)
            {
                ViewBag.Message = ex.Message;
            }
            return View("Index");
        }

        public async Task<IActionResult> KeyVaultConnection()
        {
            ViewBag.Message = "Got some data but get the key from the key vault.";
           
            try
            {
                //Create CosmosDb connection
                _cosmosDbService = await GetCosmosDbServiceUsingKeyVault();
                var results = (await _cosmosDbService.GetItemsAsync("SELECT * FROM c"));
                return View("Index", results);
            }
            catch (Exception ex) {
                ViewBag.Message = ex.Message;
            }
            return View("Index");
            
        }

        public async Task<IActionResult> ManagedIdentityConnection()
        {
            ViewBag.Message = "Got some data but get the key from the key vault.";

            try
            {
                //Create CosmosDb connection
                _cosmosDbService = await GetCosmosDbServiceUsingManagedIdentity();
                var results = (await _cosmosDbService.GetItemsAsync("SELECT * FROM c"));
                return View("Index", results);
            }
            catch (Exception ex)
            {
                ViewBag.Message = ex.Message;
            }
            return View("Index");

        }

        [ActionName("Create")]
        public async Task<IActionResult> Create() {
            return View();
        }

        [HttpPost]
        [ActionName("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAsync([Bind("Id, Name,Description,Completed")] Item item)
        {
            _cosmosDbService = await GetNetworkProtectedCosmosDbServiceWithStaticInfo();

            if (ModelState.IsValid) {
                item.Id = Guid.NewGuid().ToString();
                await _cosmosDbService.AddItemAsync(item);
                return RedirectToAction("Index");
            }
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
