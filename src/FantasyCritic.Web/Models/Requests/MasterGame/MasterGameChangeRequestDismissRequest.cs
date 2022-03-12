using System;
using System.ComponentModel.DataAnnotations;

namespace FantasyCritic.Web.Models.Requests.MasterGame
{
    public class MasterGameChangeRequestDismissRequest
    {
        [Required]
        public Guid RequestID { get; set; }
    }
}
