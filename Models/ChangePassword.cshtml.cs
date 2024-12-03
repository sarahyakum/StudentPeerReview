/*
	Written by Darya Anbar for CS 4485.0W1, Senior Design Project, Started November 13, 2024.
    Net ID: dxa200020

    This file defines the model that handles the change password functionality.
*/


using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;
using System.Data;
using StudentPeerReview.Models;

namespace StudentPR.Pages
{
    public class ChangePasswordModel : PageModel
    {
        // Error message to be displayed on the Page
        public string ErrorMessage { get; set; } = string.Empty;

        private readonly IConfiguration _config;

        public ChangePasswordModel(IConfiguration config) 
        {
            _config = config;
        }

        // Handles POST requests when the form is submitted
        public IActionResult OnPost(string OldPassword, string NewPassword, string ConfirmPassword)
        {
            // Checks if new password and confirmation password match
            if (NewPassword != ConfirmPassword) 
            {
                ErrorMessage = "Passwords Do Not Match. Try Again.";
                return Page();            
            }
            else 
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

                // Establishes a connection to the MySQL database
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string errorMessage;

                    // Creates MySQlCommand to call the stored procedure
                    using (var cmd = new MySqlCommand("change_student_password", connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Loads parameters
                        cmd.Parameters.AddWithValue("@stu_username", NetId);
                        cmd.Parameters.AddWithValue("@old_student_password", OldPassword);
                        cmd.Parameters.AddWithValue("@new_student_password", NewPassword);
                        var errorParam = new MySqlParameter("@error_message", MySqlDbType.VarChar) // Output parameter
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

                        // Retrieves student's updated password from the database
                        using (var cmd = new MySqlCommand("SELECT StuPassword FROM Student WHERE StuNetID = @NetId", connection))
                        {
                            cmd.Parameters.AddWithValue("@NetId", NetId);
                            using (var reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    // Creates student object and stores new password
                                    student = new Student 
                                    {
                                        Password = reader["StuPassword"]?.ToString() ?? string.Empty
                                    };
                                }
                            }
                        }

                        if (student != null)
                        {
                            // Stores the new password in the session and redirects to Login Page
                            HttpContext.Session.SetString("StudentPassword", student.Password);
                            TempData["SuccessMessage"] = "Success! Log in with your new password.";
                            return RedirectToPage("/Login");
                        }
                        else // Unable to create student object
                        {
                            ErrorMessage = "Student not found.";
                            return Page();
                        }
                    } 
                    // i.e. ErrorMessage != Success
                    else // Displays error message on Page
                    {
                        ErrorMessage = errorMessage;
                        return Page();
                    }
                }
            }
        }
    }
}
