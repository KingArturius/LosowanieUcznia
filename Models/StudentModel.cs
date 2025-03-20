namespace LosowanieUcznia.Models
{
    public class StudentModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int Number { get; set; }
        public bool IsPresent { get; set; }
        public bool HasBeenDrawn { get; set; }
        public string FullName => $"{FirstName} {LastName}";
    }
}
