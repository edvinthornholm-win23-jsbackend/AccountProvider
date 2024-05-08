using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccountProvider.Models;

public class VerificationRequest
{
    public string Email { get; set; } = null!;
    public string VerificationCode { get; set; } = null!;
}
