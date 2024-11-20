using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;
using System.Data;
using StudentPeerReview.Models;

namespace StudentPR.Pages
{
    public class ChangePasswordModel : PageModel
    {
        public string ErrorMessage { get; set; } = string.Empty;

        private readonly IConfiguration _config;

        public ChangePasswordModel(IConfiguration config) 
        {
            _config = config;
        }

        public IActionResult OnPost(string OldPassword, string NewPassword, string ConfirmPassword)
        {
            if (NewPassword != ConfirmPassword) 
            {
                ErrorMessage = "Passwords Do Not Match. Try Again.";
                return Page();            
            }
            else 
            {
                string connectionString = _config.GetConnectionString("DefaultConnection") ?? string.Empty;
                string? NetId = HttpContext.Session.GetString("StudentNetId");
                if (string.IsNullOrEmpty(NetId))
                {
                    ErrorMessage = "Session expired or invalid. Please log in again.";
                    return RedirectToPage("/Login");
                }

                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string errorMessage;
                    using (var cmd = new MySqlCommand("change_student_password", connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@stu_username", NetId);
                        cmd.Parameters.AddWithValue("@old_student_password", OldPassword);
                        cmd.Parameters.AddWithValue("@new_student_password", NewPassword);
                        var errorParam = new MySqlParameter("@error_message", MySqlDbType.VarChar)
                        {
                            Size = 100,
                            Direction = ParameterDirection.Output
                        };
                        cmd.Parameters.Add(errorParam);

                        cmd.ExecuteNonQuery();
                        errorMessage = errorParam.Value.ToString() ?? string.Empty;
                    }

                    if (errorMessage == "Success")
                    {
                        // Retrieve student's details
                        Student? student = null;
                        using (var cmd = new MySqlCommand("SELECT StuPassword FROM Student WHERE StuNetID = @NetId", connection))
                        {
                            cmd.Parameters.AddWithValue("@NetId", NetId);
                            using (var reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    student = new Student
                                    {
                                        Password = reader["StuPassword"]?.ToString() ?? string.Empty
                                    };
                                }
                            }
                        }

                        if (student != null)
                        {
                            // Store the student information in session
                            HttpContext.Session.SetString("StudentPassword", student.Password);
                            TempData["SuccessMessage"] = "Success! Log in with your new password.";
                            return RedirectToPage("/Login");
                        }
                        else 
                        {
                            ErrorMessage = "Student not found.";
                            return Page();
                        }
                    } 
                    else 
                    {
                        ErrorMessage = errorMessage;
                        return Page();
                    }
                }
            }
        }
    }
}
