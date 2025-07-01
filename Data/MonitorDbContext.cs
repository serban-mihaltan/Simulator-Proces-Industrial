using Microsoft.EntityFrameworkCore;
using MonitorApi.Models;

namespace MonitorApi.Data
{
    public class MonitorDbContext : DbContext
    {
        public MonitorDbContext(DbContextOptions<MonitorDbContext> options)
            : base(options) { }
        public DbSet<LogRecord> LogRecords { get; set; }
    }
}