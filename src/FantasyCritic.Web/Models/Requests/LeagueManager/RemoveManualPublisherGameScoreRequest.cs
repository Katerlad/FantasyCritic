using System;
using System.ComponentModel.DataAnnotations;

namespace FantasyCritic.Web.Models.Requests.LeagueManager
{
    public class RemoveManualPublisherGameScoreRequest
    {
        [Required]
        public Guid PublisherID { get; set; }
        [Required]
        public Guid PublisherGameID { get; set; }
    }
}
