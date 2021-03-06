using SecurityInDepthDemo.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SecurityInDepthDemo.Services
{
    public interface ICosmosDbService
    {
        Task AddItemAsync(Item item);
        Task DeleteItemAsync(string id);
        Task<Item> GetItemAsync(string id);
        Task<IEnumerable<Item>> GetItemsAsync(string queryString);
        Task UpdateItemAsync(string id, Item item);
    }
}