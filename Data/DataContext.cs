using Microsoft.EntityFrameworkCore;

namespace ASP_201.Data
{
    public class DataContext : DbContext
    {
        public DbSet<Entity.User> Users { get; set; }
        public DbSet<Entity.EmailConfirmToken> EmailConfirmTokens { get; set; }

        public DataContext(DbContextOptions options) : base(options)
        {
        }

    }
}
/* Д.З. Реалізувати відображення факту, що пошта не підтверджена на 
 * аватарці користувача у верхній частині сайту (або червоний фон, або
 * рамка, або інша позначка) при наведенні видає підказку "непідтв. пошта"
 */