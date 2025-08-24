using Services.Models.UserAccount;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Models.Driver
{
    public class ImportDriverDto : ImportUserDto
    {
        // License number will be handled separately through DriverLicense entity
    }
}
