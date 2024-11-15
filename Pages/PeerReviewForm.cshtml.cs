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

        public void OnGet()
        {
            LoadTeamMembers();
        }

        private IActionResult LoadTeamMembers()
        {   
            string connectionString = _config.GetConnectionString("DefaultConnection") ?? string.Empty;
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

