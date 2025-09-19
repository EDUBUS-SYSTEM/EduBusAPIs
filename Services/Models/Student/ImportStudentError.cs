namespace Services.Models.Student
{
    public class ImportStudentError
    {
        public int RowNumber { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string ParentEmail { get; set; } = null!;
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
