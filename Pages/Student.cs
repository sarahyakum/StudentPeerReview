using System.ComponentModel.DataAnnotations;

namespace StudentPeerReview.Models
{
    public class Student
    {
        public string NetId { get; set; }
        public string UtdId { get; set; }
        public string Name { get; set; }
    }
}