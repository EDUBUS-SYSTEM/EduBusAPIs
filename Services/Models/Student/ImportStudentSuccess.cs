namespace Services.Models.Student
{
    public class ImportStudentSuccess
    {
        public int RowNumber { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public Guid? ParentId { get; set; }
        public Guid Id { get; set; }
    }
}
