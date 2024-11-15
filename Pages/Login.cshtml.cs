using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;
using System.Data;
using StudentPeerReview.Models;

namespace StudentPR.Pages
{
    public class LoginModel : PageModel
    {
        public string ErrorMessage { get; set; } = string.Empty;

        private readonly IConfiguration _config;

        public LoginModel (IConfiguration config) 
        {
            _config = config;
        }

        public IActionResult OnPost(string NetId, string UtdId)
        {
            string connectionString = _config.GetConnectionString("DefaultConnection") ?? string.Empty;

            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                string errorMessage;
                using (var cmd = new MySqlCommand("check_student_login", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@stu_input_username", NetId);
                    cmd.Parameters.AddWithValue("@stu_input_password", UtdId);
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
                    using (var cmd = new MySqlCommand("SELECT StuNetID, StuUTDID, StuName FROM Student WHERE StuNetID = @NetId", connection))
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
                                    Name = reader["StuName"]?.ToString() ?? string.Empty
                                };
                            }
                        }
                    }

                    if (student != null)
                    {
                        // Store the student information in session
                        HttpContext.Session.SetString("StudentNetId", student.NetId);
                        HttpContext.Session.SetString("StudentUtdId", student.UtdId);
                        HttpContext.Session.SetString("StudentName", student.Name);
                        
                        return RedirectToPage("/PeerReviewForm");
                    } 
                    else 
                    {
                        ErrorMessage = "Student not found.";
                        return Page();
                    }
                } 
                else if (errorMessage == "Change password")
                {
                    HttpContext.Session.SetString("StudentNetId", NetId);
                    return RedirectToPage("/ChangePassword");
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
