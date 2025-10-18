using FabOS.WebServer.Data.Contexts;
using FabOS.WebServer.Services.Interfaces;
using FabOS.WebServer.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Microsoft.Data.SqlClient;

namespace FabOS.WebServer.Services.Implementations;

public class DatabaseService : IDatabaseService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseService> _logger;

    public DatabaseService(ApplicationDbContext context, IConfiguration configuration, ILogger<DatabaseService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            await _context.Database.OpenConnectionAsync();
            await _context.Database.CloseConnectionAsync();
            _logger.LogInformation("Database connection test successful");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database connection test failed");
            return false;
        }
    }

    public async Task<List<string>> GetTableNamesAsync()
    {
        try
        {
            var query = @"
                SELECT TABLE_NAME 
                FROM INFORMATION_SCHEMA.TABLES 
                WHERE TABLE_TYPE = 'BASE TABLE' 
                ORDER BY TABLE_NAME";

            var tables = new List<string>();
            using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            using var command = new SqlCommand(query, connection);
            
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                tables.Add(reader.GetString(0));
            }

            return tables;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving table names");
            throw;
        }
    }

    public async Task<DataTable> ExecuteQueryAsync(string query)
    {
        try
        {
            var dataTable = new DataTable();
            using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            using var command = new SqlCommand(query, connection);
            using var adapter = new SqlDataAdapter(command);
            
            await connection.OpenAsync();
            adapter.Fill(dataTable);
            
            return dataTable;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing query: {Query}", query);
            throw;
        }
    }

    public async Task<List<Dictionary<string, object>>> GetTableDataAsync(string tableName, int maxRows = 100)
    {
        try
        {
            var query = $"SELECT TOP {maxRows} * FROM [{tableName}]";
            var results = new List<Dictionary<string, object>>();
            
            using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            using var command = new SqlCommand(query, connection);
            
            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader.GetValue(i) ?? DBNull.Value;
                }
                results.Add(row);
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving data from table: {TableName}", tableName);
            throw;
        }
    }

    #region Customer Methods

    public async Task<List<Customer>> GetCustomersAsync()
    {
        try
        {
            return await _context.Customers
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customers");
            throw;
        }
    }

    public async Task<Customer> GetCustomerByIdAsync(int id)
    {
        try
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer == null)
                throw new KeyNotFoundException($"Customer with ID {id} not found");

            return customer;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customer with ID: {Id}", id);
            throw;
        }
    }

    public async Task<Customer> CreateCustomerAsync(Customer customer)
    {
        try
        {
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
            return customer;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer");
            throw;
        }
    }

    public async Task<Customer> UpdateCustomerAsync(Customer customer)
    {
        try
        {
            _context.Customers.Update(customer);
            await _context.SaveChangesAsync();
            return customer;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer with ID: {Id}", customer.Id);
            throw;
        }
    }

    public async Task<bool> DeleteCustomerAsync(int id)
    {
        try
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
                return false;

            // Soft delete
            customer.IsActive = false;
            customer.LastModified = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting customer with ID: {Id}", id);
            throw;
        }
    }

    #endregion

    #region Customer Contact Methods

    public async Task<List<CustomerContact>> GetCustomerContactsAsync(int customerId)
    {
        try
        {
            return await _context.CustomerContacts
                .Where(c => c.CustomerId == customerId && c.IsActive)
                .OrderBy(c => c.IsPrimary ? 0 : 1)
                .ThenBy(c => c.LastName)
                .ThenBy(c => c.FirstName)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving contacts for customer ID: {CustomerId}", customerId);
            throw;
        }
    }

    public async Task<CustomerContact> CreateCustomerContactAsync(CustomerContact contact)
    {
        try
        {
            _context.CustomerContacts.Add(contact);
            await _context.SaveChangesAsync();
            return contact;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer contact");
            throw;
        }
    }

    public async Task<CustomerContact> UpdateCustomerContactAsync(CustomerContact contact)
    {
        try
        {
            _context.CustomerContacts.Update(contact);
            await _context.SaveChangesAsync();
            return contact;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer contact with ID: {Id}", contact.Id);
            throw;
        }
    }

    public async Task<bool> DeleteCustomerContactAsync(int id)
    {
        try
        {
            var contact = await _context.CustomerContacts.FindAsync(id);
            if (contact == null)
                return false;

            // Soft delete
            contact.IsActive = false;
            contact.LastModified = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting customer contact with ID: {Id}", id);
            throw;
        }
    }

    #endregion

    #region Customer Address Methods

    public async Task<List<CustomerAddress>> GetCustomerAddressesAsync(int customerId)
    {
        try
        {
            return await _context.CustomerAddresses
                .Where(a => a.CustomerId == customerId && a.IsActive)
                .OrderBy(a => a.AddressType)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving addresses for customer ID: {CustomerId}", customerId);
            throw;
        }
    }

    public async Task<CustomerAddress> CreateCustomerAddressAsync(CustomerAddress address)
    {
        try
        {
            _context.CustomerAddresses.Add(address);
            await _context.SaveChangesAsync();
            return address;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer address");
            throw;
        }
    }

    public async Task<CustomerAddress> UpdateCustomerAddressAsync(CustomerAddress address)
    {
        try
        {
            _context.CustomerAddresses.Update(address);
            await _context.SaveChangesAsync();
            return address;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer address with ID: {Id}", address.Id);
            throw;
        }
    }

    public async Task<bool> DeleteCustomerAddressAsync(int id)
    {
        try
        {
            var address = await _context.CustomerAddresses.FindAsync(id);
            if (address == null)
                return false;

            // Soft delete
            address.IsActive = false;
            address.LastModified = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting customer address with ID: {Id}", id);
            throw;
        }
    }

    #endregion
}
