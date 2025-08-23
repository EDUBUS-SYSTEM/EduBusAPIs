namespace Data.Models;

public partial class Parent : UserAccount
{
    public virtual ICollection<Student> Students { get; set; } = new List<Student>();

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}

