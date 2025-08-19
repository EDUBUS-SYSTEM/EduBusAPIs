namespace Data.Models;

public partial class Admin : UserAccount
{
    public virtual ICollection<UnitPrice> UnitPrices { get; set; } = new List<UnitPrice>();

    public virtual ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}
