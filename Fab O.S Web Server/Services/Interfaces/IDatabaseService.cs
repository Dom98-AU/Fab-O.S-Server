using System.Data;
using FabOS.WebServer.Models.Entities;

namespace FabOS.WebServer.Services.Interfaces;

public interface IDatabaseService
{
    Task<bool> TestConnectionAsync();
    Task<List<string>> GetTableNamesAsync();
    Task<DataTable> ExecuteQueryAsync(string query);
    Task<List<Dictionary<string, object>>> GetTableDataAsync(string tableName, int maxRows = 100);

    // Customer methods
    Task<List<Customer>> GetCustomersAsync();
    Task<Customer> GetCustomerByIdAsync(int id);
    Task<Customer> CreateCustomerAsync(Customer customer);
    Task<Customer> UpdateCustomerAsync(Customer customer);
    Task<bool> DeleteCustomerAsync(int id);

    // Customer Contact methods
    Task<List<CustomerContact>> GetCustomerContactsAsync(int customerId);
    Task<CustomerContact> CreateCustomerContactAsync(CustomerContact contact);
    Task<CustomerContact> UpdateCustomerContactAsync(CustomerContact contact);
    Task<bool> DeleteCustomerContactAsync(int id);

    // Customer Address methods
    Task<List<CustomerAddress>> GetCustomerAddressesAsync(int customerId);
    Task<CustomerAddress> CreateCustomerAddressAsync(CustomerAddress address);
    Task<CustomerAddress> UpdateCustomerAddressAsync(CustomerAddress address);
    Task<bool> DeleteCustomerAddressAsync(int id);
}
