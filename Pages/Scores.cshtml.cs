/*
	Written by Darya Anbar for CS 4485.0W1, Senior Design Project, Started November 13, 2024.
    Net ID: dxa200020

    This file defines the model that handles the Scores Page functionality.
*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;
using System.Data;
using StudentPeerReview.Models;
using System.Text.Json;

namespace StudentPR.Pages
{
    public class ScoresModel : PageModel
    {
        // Error message to be displayed on the Page
        public string ErrorMessage { get; set; } = string.Empty;

        private readonly IConfiguration _config;

        public ScoresModel (IConfiguration config) 
        {
            _config = config;
        }
        
        // Retrieves list of criteria names from the session, if exists        
        public List<String> GetCriteriaNames()
        {
            return HttpContext.Session.GetObject<List<String>>("CriteriaNames") ?? new List<String>();
        }

        // Retrieves list of average scores from the session, if exists        
        public List<decimal> GetAverageScores()
        {
            return HttpContext.Session.GetObject<List<decimal>>("AverageScores") ?? new List<decimal>();
        }

        // Retrieves the review type from the session, if exists        
        public String GetReviewType()
        {
            return HttpContext.Session.GetString("ScoresAvailability") ?? string.Empty;
        }

        // Handles GET requests
        public IActionResult OnGet()
        {
            // Checks if user is logged in and redirects to Login Page, if not
            var loggedInStatus = HttpContext.Session.GetString("LoggedIn");
            if (loggedInStatus == null)
            {
                TempData["ErrorMessage"] = "You must log in to access this page.";
                return RedirectToPage("/Login");
            }

            // Checks if scores are available and redirects to appropriate Page, if so
            var availability = HttpContext.Session.GetString("ScoresAvailability");
            if (availability == "Unavailable")
            {
                return RedirectToPage("/ScoresUnavailable");
            }
            
            LoadScores();
            return Page();
        }

        // Loads average scores from the database and stores them in the session
        private IActionResult LoadScores()
        {
            // Retrieves database connection string from configuration
            string connectionString = _config.GetConnectionString("DefaultConnection") ?? string.Empty;
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentException("Connection string 'DefaultConnection' is not set.");
            }
            
            // Retrieves student's NetId and section code from the session 
            string? NetId = HttpContext.Session.GetString("StudentNetId");
            if (string.IsNullOrEmpty(NetId))
            {
                ErrorMessage = "Session expired or invalid. Please log in again.";
                return RedirectToPage("/Login");
            }
            
            string? SecCode = HttpContext.Session.GetString("SectionCode");
            if (string.IsNullOrEmpty(SecCode))
            {
                ErrorMessage = "Session expired or invalid. Please log in again.";
                return RedirectToPage("/Login");
            }


            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                // Creates MySQlCommand to call the stored procedure
                using (var cmd = new MySqlCommand("student_view_averages", connection))
                {
                    string ReviewType = HttpContext.Session.GetString("ScoresAvailability") ?? string.Empty;
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Loads parameters
                    cmd.Parameters.AddWithValue("@stu_netID", NetId);
                    cmd.Parameters.AddWithValue("@section_code", SecCode);
                    cmd.Parameters.AddWithValue("@review_type", ReviewType);

                     List<String> criteriaNames = new();
                     List<decimal> averageScores = new();
                    
                    // Executes the command and reads the results
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            criteriaNames.Add(reader["CriteriaName"]?.ToString() ?? string.Empty);
                            if (decimal.TryParse(reader["AvgScore"]?.ToString(), out decimal score))
                            {
                                averageScores.Add(score);
                            }
                            else
                            {
                                averageScores.Add(0);   // If TeamNum cannot be parsed to a decimal
                            }
                        }
                    }
                    
                    // Stores retrieved criteria and scores in the session
                    HttpContext.Session.SetObject("CriteriaNames", criteriaNames);
                    HttpContext.Session.SetObject("AverageScores", averageScores);
                }
            }
            return Page();
        }
    }
}
