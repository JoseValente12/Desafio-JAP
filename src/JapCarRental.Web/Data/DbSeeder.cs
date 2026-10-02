using JapCarRental.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace JapCarRental.Web.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Vehicles.AnyAsync() || await db.Customers.AnyAsync())
        {
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.Today);

        var vehicles = new[]
        {
            new Vehicle("Renault", "Clio", "AA11BB", 2021, FuelType.Petrol),
            new Vehicle("Peugeot", "308", "CCC22DD", 2022, FuelType.Diesel),
            new Vehicle("Tesla", "Model 3", "EE33FF", 2023, FuelType.Electric),
            new Vehicle("Toyota", "Corolla", "GGG44HH", 2022, FuelType.Hybrid),
            new Vehicle("Fiat", "Panda", "III55JJ", 2020, FuelType.Lpg),
            new Vehicle("Volkswagen", "Golf", "KKK66LL", 2019, FuelType.Diesel),
            new Vehicle("Kia", "Ceed", "MMM77NN", 2021, FuelType.Petrol),
            new Vehicle("Hyundai", "Kona", "OOO88PP", 2023, FuelType.Electric),
            new Vehicle("Ford", "Focus", "QQQ99RR", 2020, FuelType.Diesel),
            new Vehicle("Seat", "Ibiza", "SSS10TT", 2022, FuelType.Petrol),
        };

        var customers = new[]
        {
            new Customer("Ana Teste Silva", "ana.silva@example.com", "910000001", "P-1000001"),
            new Customer("Bruno Exemplo Costa", "bruno.costa@example.com", "910000002", "P-1000002"),
            new Customer("Carla Demo Pereira", "carla.pereira@example.com", "910000003", "P-1000003"),
            new Customer("Diogo Ficticio Santos", "diogo.santos@example.com", "910000004", "P-1000004"),
            new Customer("Eva Amostra Lopes", "eva.lopes@example.com", "910000005", "P-1000005"),
            new Customer("Filipe Ficticio Rocha", "filipe.rocha@example.com", "910000006", "P-1000006"),
            new Customer("Graca Amostra Nunes", "graca.nunes@example.com", "910000007", "P-1000007"),
            new Customer("Hugo Demo Matos", "hugo.matos@example.com", "910000008", "P-1000008"),
        };

        db.Vehicles.AddRange(vehicles);
        db.Customers.AddRange(customers);
        await db.SaveChangesAsync(); // generates the Ids

        var contracts = new[]
        {
            // Active today
            new RentalContract(customers[0].Id, vehicles[0].Id, today.AddDays(-3), today.AddDays(4), 42000),
            new RentalContract(customers[1].Id, vehicles[3].Id, today.AddDays(-1), today.AddDays(6), 18500),
            new RentalContract(customers[2].Id, vehicles[5].Id, today.AddDays(-5), today.AddDays(2), 87000),
            new RentalContract(customers[4].Id, vehicles[7].Id, today, today.AddDays(3), 6200),

            // Past (one on a vehicle that is also rented now)
            new RentalContract(customers[3].Id, vehicles[0].Id, today.AddDays(-30), today.AddDays(-25), 41200),
            new RentalContract(customers[3].Id, vehicles[1].Id, today.AddDays(-20), today.AddDays(-15), 31000),

            // Future
            new RentalContract(customers[0].Id, vehicles[2].Id, today.AddDays(10), today.AddDays(14), 8500),
            new RentalContract(customers[5].Id, vehicles[6].Id, today.AddDays(5), today.AddDays(9), 23000),
        };

        db.RentalContracts.AddRange(contracts);
        await db.SaveChangesAsync();
    }
}