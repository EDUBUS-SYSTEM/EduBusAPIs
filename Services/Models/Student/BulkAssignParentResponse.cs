namespace Services.Models.Student
{
    public class BulkAssignParentResponse
    {
        public bool Success { get; set; }
        public int UpdatedCount { get; set; }
        public List<StudentDto> Students { get; set; } = new();
        public string Message { get; set; } = string.Empty;
    }
}
