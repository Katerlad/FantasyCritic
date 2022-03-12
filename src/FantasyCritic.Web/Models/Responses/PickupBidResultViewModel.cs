using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FantasyCritic.Lib.Domain.Results;

namespace FantasyCritic.Web.Models.Responses
{
    public class PickupBidResultViewModel
    {
        public PickupBidResultViewModel(ClaimResult domain)
        {
            Success = domain.Success;
            Errors = domain.Errors.Select(x => x.Error).ToList();
        }

        public bool Success { get; }
        public IReadOnlyList<string> Errors { get; }
    }
}
