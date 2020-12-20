using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FantasyCritic.Web.Models.Requests.Admin
{
    public class AdminResendConfirmationEmail
    {
        [Required]
        public Guid UserID { get; set; }
    }
}
