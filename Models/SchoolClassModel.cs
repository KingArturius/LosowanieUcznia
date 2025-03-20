namespace LosowanieUcznia.Models
{
    public class SchoolClassModel
    {
        public string ClassName { get; set; }
        public List<StudentModel> Students { get; set; } = new List<StudentModel>();
    }
}
