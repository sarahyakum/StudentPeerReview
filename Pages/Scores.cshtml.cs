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
        public string ErrorMessage { get; set; } = string.Empty;

        private readonly IConfiguration _config;

        public ScoresModel (IConfiguration config) 
        {
            _config = config;
        }

        public List<String> GetCriteriaNames()
        {
            return HttpContext.Session.GetObject<List<String>>("CriteriaNames") ?? new List<String>();
        }

         public List<decimal> GetAverageScores()
        {
            return HttpContext.Session.GetObject<List<decimal>>("AverageScores") ?? new List<decimal>();
        }

        public IActionResult OnGet()
        {
            var loggedInStatus = HttpContext.Session.GetString("LoggedIn");
            if (loggedInStatus == null)
            {
                TempData["ErrorMessage"] = "You must log in to access this page.";
                return RedirectToPage("/Login");
            }

            var availability = HttpContext.Session.GetString("ScoresAvailability");
            if (availability == "Unavailable")
            {
                return RedirectToPage("/ScoresUnavailable");
            }
            
            LoadScores();
            return Page();
        }

        private IActionResult LoadScores()
        {
            string connectionString = _config.GetConnectionString("DefaultConnection") ?? string.Empty;
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentException("Connection string 'DefaultConnection' is not set.");
            }
            
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

                using (var cmd = new MySqlCommand("student_view_averages", connection))
                {
                    string ReviewType = HttpContext.Session.GetString("ScoresAvailability") ?? string.Empty;
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@stu_netID", NetId);
                    cmd.Parameters.AddWithValue("@section_code", SecCode);
                    cmd.Parameters.AddWithValue("@review_type", ReviewType);

                     List<String> criteriaNames = new();
                     List<decimal> averageScores = new();
                    
                    // Execute the command and read the results
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
                                averageScores.Add(0);
                            }
                        }
                    }
                    
                    HttpContext.Session.SetObject("CriteriaNames", criteriaNames);
                    HttpContext.Session.SetObject("AverageScores", averageScores);
                }
            }
            return Page();
        }
    }
}
