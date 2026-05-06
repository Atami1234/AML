using AmlDetectionApi.Models;
using Microsoft.EntityFrameworkCore;

namespace AmlDetectionApi.Data
{
    public static class DbInitializer
    {
        public static void Initialize(AmlDbContext context)
        {
            // Note: In a production app, you might use context.Database.Migrate() here
            // but for this MVP we assume the user runs migrations manually first.
            
            if (context.Customers.Any())
            {
                return;   // DB has been seeded
            }

            var customers = new Customer[]
            {
                new Customer{FullName="John Doe",NationalId="N123456",RiskLevel="Low"},
                new Customer{FullName="Alice Smith",NationalId="N789012",RiskLevel="Medium"},
                new Customer{FullName="Bob Johnson",NationalId="N345678",RiskLevel="High"}
            };
            context.Customers.AddRange(customers);
            context.SaveChanges();

            var accounts = new Account[]
            {
                new Account{AccountNumber="ACC101",CustomerId=customers[0].CustomerId,AccountType="Checking",Balance=50000},
                new Account{AccountNumber="ACC102",CustomerId=customers[1].CustomerId,AccountType="Savings",Balance=25000},
                new Account{AccountNumber="ACC103",CustomerId=customers[2].CustomerId,AccountType="Checking",Balance=100000},
                new Account{AccountNumber="ACC104",CustomerId=customers[0].CustomerId,AccountType="Savings",Balance=5000}
            };
            context.Accounts.AddRange(accounts);
            context.SaveChanges();
        }
    }
}
