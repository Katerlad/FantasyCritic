using System;
using System.ComponentModel.DataAnnotations;

namespace FantasyCritic.Web.Models.Requests.LeagueManager
{
    public class CreateInviteRequest
    {
        [Required]
        public Guid LeagueID { get; set; }
        public string InviteEmail { get; set; }
        public string InviteDisplayName { get; set; }
        public int? InviteDisplayNumber { get; set; }

        public bool IsValid()
        {
            if (IsDisplayNameInvite())
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(InviteEmail))
            {
                return true;
            }

            return false;
        }

        public bool IsDisplayNameInvite()
        {
            if (!string.IsNullOrWhiteSpace(InviteDisplayName) && InviteDisplayNumber != null)
            {
                return true;
            }

            return false;
        }
    }
}
