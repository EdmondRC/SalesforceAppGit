using Microsoft.AspNetCore.Mvc.RazorPages;
using SalesforceApp.Repositories;
using SalesforceApp.Models;

namespace SalesforceApp.Pages
{
    public class AccountsModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public AccountsModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public List<AccountRecord> Accounts { get; set; } = new();

        public void OnGet()
        {
            string connectionString =
            _configuration.GetConnectionString("DefaultConnection")!;

            var repo = new AccountRepository(connectionString);

            Accounts = repo.GetAccounts();
        }
    }
}