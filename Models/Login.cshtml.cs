/*
	Written by Darya Anbar for CS 4485.0W1, Senior Design Project, Started November 13, 2024.
    Net ID: dxa200020

    This file defines the model that handles a student logging into the application.
*/


using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;
using System.Data;
using StudentPeerReview.Models;

namespace StudentPR.Pages
{
    public class LoginModel : PageModel
    {
        // Error message to be displayed on the Page
        public string ErrorMessage { get; set; } = string.Empty;

        private readonly IConfiguration _config;

        public LoginModel (IConfiguration config) 
        {
            _config = config;
        }

        // Handles GET requests
        // If a student is currently logged in, Login Page redirects to Logout Page
        public IActionResult OnGet()
        {
            if (HttpContext.Session.GetString("LoggedIn") != null) // User is logged in
            {
                return RedirectToPage("/Logout");
            }
            return Page();
        }

        // Handles POST requests 
        public IActionResult OnPost(string NetId, string UtdId)
        {
            // Retrieves database connection string from configuration
            string connectionString = _config.GetConnectionString("DefaultConnection") ?? string.Empty;
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentException("Connection string 'DefaultConnection' is not set.");
            }

            // Establishes a connection to the MySQL database
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string errorMessage;

                // Creates MySQlCommand to call the stored procedure
                using (var cmd = new MySqlCommand("check_student_login", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Loads Parameters
                    cmd.Parameters.AddWithValue("@stu_input_username", NetId);
                    cmd.Parameters.AddWithValue("@stu_input_password", UtdId);
                    var errorParam = new MySqlParameter("@error_message", MySqlDbType.VarChar)  // Output parameter
                    {
                        Size = 100,
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(errorParam);

                    cmd.ExecuteNonQuery(); // Calls stored procedure
                    errorMessage = errorParam.Value.ToString() ?? string.Empty; // Retrieves output error message
                }

                if (errorMessage == "Success")
                {
                    Student? student = null;

                    // Retrieves student's details from database
                    using (var cmd = new MySqlCommand("SELECT S.StuNetID, S.StuUTDID, S.StuName, M.SecCode, M.TeamNum " 
                                                        + "FROM Student S "
                                                        + "INNER JOIN MemberOf M ON S.StuNetID = M.StuNetID " 
                                                        + "WHERE S.StuNetID = @NetId", connection))
                    {
                        cmd.Parameters.AddWithValue("@NetId", NetId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                student = new Student
                                {
                                    NetId = reader["StuNetID"]?.ToString() ?? string.Empty,
                                    UtdId = reader["StuUTDID"]?.ToString() ?? string.Empty,
                                    Name = reader["StuName"]?.ToString() ?? string.Empty,
                                    Section = reader["SecCode"]?.ToString() ?? string.Empty,
                                    TeamNum = reader["TeamNum"]?.ToString() ?? string.Empty
                                };
                            }
                        }
                    }

                    if (student != null)
                    {
                        // Stores student's information in the session and redirects to Peer Review Form Page
                        HttpContext.Session.SetString("StudentNetId", student.NetId);
                        HttpContext.Session.SetString("StudentUtdId", student.UtdId);
                        HttpContext.Session.SetString("StudentName", student.Name);
                        HttpContext.Session.SetString("SectionCode", student.Section);
                        HttpContext.Session.SetString("TeamNumber", student.TeamNum);
                        HttpContext.Session.SetString("LoggedIn", student.NetId);
                        HttpContext.Session.SetString("PRAvailability", GetPRAvailability(student.NetId, student.Section));
                        HttpContext.Session.SetString("ScoresAvailability", GetScoresAvailability(student.NetId, student.Section));
                        
                        return RedirectToPage("/PRForm");
                    } 
                    else  // Unable to create student object
                    {
                        ErrorMessage = "Student not found.";
                        return Page();
                    }
                } 
                // Password needs to be changed
                else if (errorMessage == "Change password")
                {
                    HttpContext.Session.SetString("StudentNetId", NetId);
                    return RedirectToPage("/ChangePassword");
                }
                // i.e. ErrorMessage != Success
                else // Displays error message on Page
                {
                    ErrorMessage = errorMessage;
                    return Page();
                }
            }
        }


        // Retrieves the availability of any peer reviews for a specific student
        // Input: Student NetId, Section Code
        // Output: Error Message returned by the stored procedure containing the availability (if any)
        private string GetPRAvailability(string NetId, string SecCode)
        {
            string connectionString = _config.GetConnectionString("DefaultConnection") ?? string.Empty;
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentException("Connection string 'DefaultConnection' is not set.");
            }

            // Establishes a connection to the MySQL database 
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string errorMessage;

                // Calls stored procedure to get the availability
                using (var cmd = new MySqlCommand("check_peer_review_availability", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@student_netID", NetId);
                    cmd.Parameters.AddWithValue("@section_code", SecCode);
                    var errorParam = new MySqlParameter("@error_message", MySqlDbType.VarChar)
                    {
                        Size = 100,
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(errorParam);
                    cmd.ExecuteNonQuery();
                    errorMessage = errorParam.Value.ToString() ?? string.Empty;
                }
                return errorMessage;
            }
        }


        // Retrieves the availability of peer review scores for a specific student
        // Input: Student NetId, Section Code
        // Output: Error Message returned by the stored procedure containing the availability (if any)
        private string GetScoresAvailability(string NetId, string SecCode)
        {
            string connectionString = _config.GetConnectionString("DefaultConnection") ?? string.Empty;
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentException("Connection string 'DefaultConnection' is not set.");
            }

            // Establishes a connection to the MySQL database 
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string errorMessage;

                // Calls stored procedure to get the availability
                using (var cmd = new MySqlCommand("check_scores_availability", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@student_netID", NetId);
                    cmd.Parameters.AddWithValue("@section_code", SecCode);
                    var errorParam = new MySqlParameter("@error_message", MySqlDbType.VarChar)
                    {
                        Size = 100,
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(errorParam);
                    cmd.ExecuteNonQuery();
                    errorMessage = errorParam.Value.ToString() ?? string.Empty;
                }
                return errorMessage;
            }
        }
    }
}
