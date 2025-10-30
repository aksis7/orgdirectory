using Microsoft.EntityFrameworkCore;
using OrgDirectory.Web.Models;

namespace OrgDirectory.Web.Data;

public static class SeedData
{
    public static async Task EnsureSeedAsync(AppDbContext db)
    {
                if (!await db.Activities.AnyAsync())
        {
            var it = new Activity { Name = "Информационные технологии" };
            var finance = new Activity { Name = "Финансы" };
            var retail = new Activity { Name = "Ритейл" };

            db.Activities.AddRange(it, finance, retail);

            var c1 = new Citizen { FirstName = "Иван", LastName = "Иванов", MiddleName = "Иванович", BirthYear = 1985, Gender = "M", RegistrationAddress = "Москва", Inn = "7700000001", Snils = "123-456-789 00" };
            var c2 = new Citizen { FirstName = "Мария", LastName = "Петрова", MiddleName = "Сергеевна", BirthYear = 1990, Gender = "F", RegistrationAddress = "Санкт-Петербург", Inn = "7800000002", Snils = "321-654-987 11" };
            db.Citizens.AddRange(c1, c2);

            db.Organizations.AddRange(
                new Organization { FullName = "ООО «АйТи-Стар»", ShortName = "АйТи-Стар", Activity = it, Director = c1, CharterCapital = 100000, Inn = "7701234567", Kpp = "770101001", Ogrn = "1167746123456" },
                new Organization { FullName = "АО «ФинГрупп»", ShortName = "ФинГрупп", Activity = finance, Director = c2, CharterCapital = 5000000, Inn = "7707654321", Kpp = "770701002", Ogrn = "1027700132456" }
            );
        }

        await db.SaveChangesAsync();
    }
}
