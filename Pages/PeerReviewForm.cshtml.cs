using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;
using System.Data;
using StudentPeerReview.Models;
using System.Text.Json;


namespace StudentPR.Pages
{
    public class PeerReviewFormModel : PageModel
    {
        public string ErrorMessage { get; set; } = string.Empty;

        private readonly IConfiguration _config;

        public PeerReviewFormModel(IConfiguration config) 
        {
            _config = config;
        }

        public List<Student> GetTeamMembers()
        {
            return HttpContext.Session.GetObject<List<Student>>("TeamMembers") ?? new List<Student>();
        }

        public List<String> GetCriteriaNames()
        {
            return HttpContext.Session.GetObject<List<String>>("CriteriaNames") ?? new List<String>();
        }

        public List<String> GetCriteriaDescriptions()
        {
            return HttpContext.Session.GetObject<List<String>>("CriteriaDescriptions") ?? new List<String>();
        }

        public IActionResult OnGet()
        {
            if (HttpContext.Session.GetString("LoggedIn") == null)
            {
                TempData["ErrorMessage"] = "You must log in to access this page.";
                return RedirectToPage("/Login");
            }

            LoadTeamMembers();
            return Page();
        }

        public IActionResult OnPost()
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
            var teamMembers = GetTeamMembers();
            var criteriaNames = GetCriteriaNames();
            var scores = new List<(string RevieweeNetId, string CriteriaName, int Score)>();

            // validates scores and stores them
            foreach (var member in teamMembers)
            {
                foreach (var criteria in criteriaNames)
                {
                    string formFieldName = $"{criteria}-{member.NetId}";
                    if (!Request.Form.TryGetValue(formFieldName, out var scoreValue) || string.IsNullOrEmpty(scoreValue))
                    {
                        ErrorMessage = $"Please fill out all scores before submitting.";
                        return Page();
                    }
                    if (int.TryParse(scoreValue, out int score))
                    {
                        scores.Add((member.NetId, criteria, score));  // stores each respective reviewee, criteria, score 
                    }
                    else
                    {
                        ErrorMessage = $"Invalid score entered for {member.Name} in {criteria}.";
                        return Page();
                    }
                }
            }

            // uses a transaction to roll back all operations in the event of a failure 
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                
                using (var transaction = connection.BeginTransaction()) // begin transaction
                {
                    try
                    {
                        foreach (var (revieweeNetId, criteriaName, score) in scores)
                        {
                            string errorMessage;
                            using (var cmd = new MySqlCommand("student_insert_score", connection, transaction)) // passes transaction here
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.Parameters.AddWithValue("@section_code", SecCode);
                                cmd.Parameters.AddWithValue("@reviewer_netID", NetId);
                                cmd.Parameters.AddWithValue("@reviewee_netID", revieweeNetId);
                                cmd.Parameters.AddWithValue("@criteria_name", criteriaName);
                                cmd.Parameters.AddWithValue("@review_type", "Midterm");     // CHANGE THIS
                                cmd.Parameters.AddWithValue("@updated_score", score);
                                var errorParam = new MySqlParameter("@error_message", MySqlDbType.VarChar)
                                {
                                    Size = 100,
                                    Direction = ParameterDirection.Output
                                };
                                cmd.Parameters.Add(errorParam);

                                cmd.ExecuteNonQuery();
                                errorMessage = errorParam.Value.ToString() ?? string.Empty;

                                if (errorMessage != "Success")
                                {
                                    throw new Exception(errorMessage); // trigger rollback
                                }
                            }
                        }
                        transaction.Commit(); // commits transaction if all inserts succeeded
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback(); // rollback all operations if an error occurs
                        ErrorMessage = $"Failed to submit scores: {ex.Message}";
                        return Page(); 
                    }
                }
            }     
            // Success
            return RedirectToPage("/PeerReviewSuccess");
        }


        private IActionResult LoadTeamMembers()
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
            string? TeamNum = HttpContext.Session.GetString("TeamNumber");

            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                using (var cmd = new MySqlCommand("student_get_peer_review_criteria", connection))
                {
                    string ReviewType = "Midterm"; // CHANGE THIS
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@stu_netID", NetId);
                    cmd.Parameters.AddWithValue("@review_type", ReviewType);
                    cmd.Parameters.AddWithValue("@section_code", SecCode);

                     List<String> criteriaNames = new();
                     List<String> criteriaDescriptions = new();
                    
                    // Execute the command and read the results
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            criteriaNames.Add(reader["CriteriaName"]?.ToString() ?? string.Empty);
                            criteriaDescriptions.Add(reader["CriteriaDescription"]?.ToString() ?? string.Empty);
                        }
                    }
                    HttpContext.Session.SetObject("CriteriaNames", criteriaNames);
                    HttpContext.Session.SetObject("CriteriaDescriptions", criteriaDescriptions);
                }

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
                    
                    // Execute the command and read the results
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
                    HttpContext.Session.SetObject("TeamMembers", teamMembers);
                }
            }
            return Page();
        }
    }


    public static class SessionExtensions
    {
        public static void SetObject<T>(this ISession session, string key, T value)
        {
            session.SetString(key, JsonSerializer.Serialize(value));
        }

        public static T? GetObject<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default : JsonSerializer.Deserialize<T>(value);
        }
    }
}

