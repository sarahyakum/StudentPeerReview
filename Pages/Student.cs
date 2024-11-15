using System.ComponentModel.DataAnnotations;

namespace StudentPeerReview.Models
{
    public class Student
    {
        public string NetId { get; set; } = string.Empty;
        public string UtdId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty;
        public string TeamNum { get; set; } = string.Empty;
    }
}