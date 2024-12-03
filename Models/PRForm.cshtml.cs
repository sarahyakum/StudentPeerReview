/*
	Written by Darya Anbar for CS 4485.0W1, Senior Design Project, Started November 13, 2024.
    Net ID: dxa200020

    This file defines the model that handles the peer review form functionality.
*/


using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;
using System.Data;
using StudentPeerReview.Models;
using System.Text.Json;


namespace StudentPR.Pages
{
    public class PRFormModel : PageModel
    {
        // Error message to be displayed on the Page
        public string ErrorMessage { get; set; } = string.Empty;

        private readonly IConfiguration _config;

        public PRFormModel (IConfiguration config) 
        {
            _config = config;
        }

        // Retrieves list of team members from the session, if exists        
        public List<Student> GetTeamMembers()
        {
            return HttpContext.Session.GetObject<List<Student>>("TeamMembers") ?? new List<Student>();
        }

        // Retrieves list of criteria names from the session, if exists
        public List<String> GetCriteriaNames()
        {
            return HttpContext.Session.GetObject<List<String>>("CriteriaNames") ?? new List<String>();
        }

        // Retrieves list of criteria descriptions from the session, if exists
        public List<String> GetCriteriaDescriptions()
        {
            return HttpContext.Session.GetObject<List<String>>("CriteriaDescriptions") ?? new List<String>();
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

            // Checks peer review availability and redirects to appropriate Page
            var availability = HttpContext.Session.GetString("PRAvailability");
            if (availability == "Completed")
            {
                return RedirectToPage("/PRSuccess");
            }
            else if (availability == "Unavailable")
            {
                return RedirectToPage("/PRUnavailable");
            }

            LoadTeamMembers();
            return Page();
        }

        // Handles POST requests to submit the peer review scores
        public IActionResult OnPost()
        {
            // Retrieves database connection string from configuration
            string connectionString = _config.GetConnectionString("DefaultConnection") ?? string.Empty;
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentException("Connection string 'DefaultConnection' is not set.");
            }

            // Retrieves student's NetId from the session 
            string? NetId = HttpContext.Session.GetString("StudentNetId");
            if (string.IsNullOrEmpty(NetId))
            {
                ErrorMessage = "Session expired or invalid. Please log in again.";
                return RedirectToPage("/Login");
            }
            
            // Retrieves section code, team members, and criteria names from the session 
            string? SecCode = HttpContext.Session.GetString("SectionCode");
            var teamMembers = GetTeamMembers();
            var criteriaNames = GetCriteriaNames();
            var scores = new List<(string RevieweeNetId, string CriteriaName, int Score)>();

            // Validates and stores the peer review scores for each team member and criteria
            foreach (var member in teamMembers)
            {
                foreach (var criteria in criteriaNames)
                {
                    string formFieldName = $"{criteria}-{member.NetId}";

                    // Error Message if not all fields are filled out
                    if (!Request.Form.TryGetValue(formFieldName, out var scoreValue) || string.IsNullOrEmpty(scoreValue))
                    {
                        ErrorMessage = $"Please fill out all scores before submitting.";
                        return Page();
                    }
                    if (int.TryParse(scoreValue, out int score))
                    {
                        scores.Add((member.NetId, criteria, score));  // Stores each respective reviewee, criteria, score 
                    }
                    else
                    {
                        ErrorMessage = $"Invalid score entered for {member.Name} in {criteria}.";
                        return Page();
                    }
                }
            }

            // Establishes a connection to the MySQL database
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                // Uses a transaction to roll back all operations in the event of a failure 
                using (var transaction = connection.BeginTransaction()) // Begin transaction
                {
                    try
                    {
                        // Iterates over each score and inserts it into the database
                        foreach (var (revieweeNetId, criteriaName, score) in scores)
                        {
                            string errorMessage;
                            using (var cmd = new MySqlCommand("student_insert_score", connection, transaction)) // Passes transaction here
                            {
                                string ReviewType = HttpContext.Session.GetString("PRAvailability") ?? string.Empty;
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.AddWithValue("@section_code", SecCode);
                                cmd.Parameters.AddWithValue("@reviewer_netID", NetId);
                                cmd.Parameters.AddWithValue("@reviewee_netID", revieweeNetId);
                                cmd.Parameters.AddWithValue("@criteria_name", criteriaName);
                                cmd.Parameters.AddWithValue("@review_type", ReviewType);
                                cmd.Parameters.AddWithValue("@updated_score", score);
                                var errorParam = new MySqlParameter("@error_message", MySqlDbType.VarChar)
                                {
                                    Size = 100,
                                    Direction = ParameterDirection.Output
                                };
                                cmd.Parameters.Add(errorParam);

                                cmd.ExecuteNonQuery();
                                errorMessage = errorParam.Value.ToString() ?? string.Empty;

                                // Checks if the insertion was successful, otherwise triggers a rollback
                                if (errorMessage != "Success")
                                {
                                    throw new Exception(errorMessage);
                                }
                            }
                        }
                        transaction.Commit(); // Commits transaction if all inserts succeeded
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback(); // Rollbacks all operations if an error occurs
                        ErrorMessage = $"Failed to submit scores: {ex.Message}";
                        return Page(); 
                    }
                }
            }     
            // Successfully completed submission
            HttpContext.Session.SetString("PRAvailability", "Completed");
            return RedirectToPage("/PRSuccess");
        }


        // Loads team members and peer review criteria from the database and stores them in the session
        private IActionResult LoadTeamMembers()
        {   
            // Retrieves database connection string from configuration
            string connectionString = _config.GetConnectionString("DefaultConnection") ?? string.Empty;
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentException("Connection string 'DefaultConnection' is not set.");
            }
            
            // Retrieves student's NetId from the session 
            string? NetId = HttpContext.Session.GetString("StudentNetId");
            if (string.IsNullOrEmpty(NetId))
            {
                ErrorMessage = "Session expired or invalid. Please log in again.";
                return RedirectToPage("/Login");
            }
            
            // Retrieves section code and team number from the session 
            string? SecCode = HttpContext.Session.GetString("SectionCode");
            string? TeamNum = HttpContext.Session.GetString("TeamNumber");

            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                // Creates MySQlCommand to call the stored procedure
                using (var cmd = new MySqlCommand("student_get_peer_review_criteria", connection))
                {
                    string ReviewType = HttpContext.Session.GetString("PRAvailability") ?? string.Empty;
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Loads parameters
                    cmd.Parameters.AddWithValue("@stu_netID", NetId);
                    cmd.Parameters.AddWithValue("@review_type", ReviewType);
                    cmd.Parameters.AddWithValue("@section_code", SecCode);

                    List<String> criteriaNames = new();
                    List<String> criteriaDescriptions = new();
                    
                    // Executes the command and reads the results
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            criteriaNames.Add(reader["CriteriaName"]?.ToString() ?? string.Empty);
                            criteriaDescriptions.Add(reader["CriteriaDescription"]?.ToString() ?? string.Empty);
                        }
                    }

                    // Stores retrieved criteria in the session
                    HttpContext.Session.SetObject("CriteriaNames", criteriaNames);
                    HttpContext.Session.SetObject("CriteriaDescriptions", criteriaDescriptions);
                }

                // Creates MySQlCommand to call the stored procedure
                using (var cmd = new MySqlCommand("student_get_team_members", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    if (int.TryParse(TeamNum, out int teamNumber))
                    {
                        cmd.Parameters.AddWithValue("@team_num", teamNumber);
                    }
                    else
                    {
                        // Handles error if TeamNum cannot be parsed to an integer
                        ErrorMessage = "Invalid team number.";
                        return Page();
                    }
                    cmd.Parameters.AddWithValue("@section_code", SecCode);

                    List<Student> teamMembers = new();
                    
                    // Executes the command and reads the results
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            teamMembers.Add(new Student
                            {
                                Name = reader["StuName"]?.ToString() ?? string.Empty,
                                NetId = reader["StuNetID"]?.ToString() ?? string.Empty
                            });
                        }
                    }

                    // Stores the team members in the session
                    HttpContext.Session.SetObject("TeamMembers", teamMembers);
                }
            }
            return Page();
        }
    }


    // Class for extension methods for storing and retrieving objects from the session
    public static class SessionExtensions
    {
        // Serializes and stores an object in the session
        public static void SetObject<T>(this ISession session, string key, T value)
        {
            session.SetString(key, JsonSerializer.Serialize(value));
        }

        // Retrieves and deserializes an object from the session
        public static T? GetObject<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default : JsonSerializer.Deserialize<T>(value);
        }
    }
}

