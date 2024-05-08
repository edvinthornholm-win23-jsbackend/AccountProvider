using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccountProvider.Models;

public class UserRegistrationRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
}
