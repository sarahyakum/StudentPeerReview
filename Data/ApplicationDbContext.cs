/*
	Written by Darya Anbar for CS 4485.0W1, Senior Design Project, Started November 13, 2024.
    Net ID: dxa200020

    This file defines the database context for the application.
*/

using Microsoft.EntityFrameworkCore; 
using StudentPeerReview.Models;   

namespace StudentPeerReview.Data
{
    public class ApplicationDbContext : DbContext
    {
        /*
        Constructor: passes DbContextOptions to the base class (DbContext) 
        for database context configuration (via dependency injection)
        */
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
            : base(options) 
        {
        }

        // Collection of all Student entities in the context
        public DbSet<Student> Students { get; set; } = null!;  // null! ensures non-nullability
    }
}
