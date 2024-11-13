using Microsoft.EntityFrameworkCore;
using StudentPeerReview.Models;

namespace StudentPeerReview.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) {}

        public DbSet<Student> Students { get; set; }
    }
}
