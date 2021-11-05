using System;
using System.Collections.Generic;
using System.Security.Claims;
using NodaTime;

namespace FantasyCritic.Lib.Identity
{
    public class FantasyCriticUser : IEquatable<FantasyCriticUser>
    {
        public FantasyCriticUser(Guid userID, string displayName, int displayNumber, string emailAddress, string normalizedEmailAddress, 
            bool emailConfirmed, string securityStamp, string passwordHash, Instant lastChangedCredentials, bool isDeleted)
        {
            UserID = userID;
            DisplayName = displayName;
            DisplayNumber = displayNumber;
            EmailAddress = emailAddress;
            NormalizedEmailAddress = normalizedEmailAddress;
            EmailConfirmed = emailConfirmed;
            SecurityStamp = securityStamp;
            PasswordHash = passwordHash;
            LastChangedCredentials = lastChangedCredentials;
            IsDeleted = isDeleted;
        }

        public Guid UserID { get; set; }
        public string DisplayName { get; set; }
        public int DisplayNumber { get; set; }
        public string EmailAddress { get; set; }
        public string NormalizedEmailAddress { get; set; }
        public bool EmailConfirmed { get; set; }
        public string SecurityStamp { get; set; }
        public string PasswordHash { get; set; }
        public Instant LastChangedCredentials { get; set; }
        public bool IsDeleted { get; set; }

        public void UpdateLastUsedCredentials(Instant currentInstant)
        {
            LastChangedCredentials = currentInstant;
        }

        public IReadOnlyList<Claim> GetUserClaims(IEnumerable<string> roles)
        {
            var usersClaims = new List<Claim>()
            {
                new Claim(ClaimTypes.Name, NormalizedEmailAddress),
                new Claim(ClaimTypes.NameIdentifier, UserID.ToString()),
            };

            foreach (var role in roles)
            {
                usersClaims.Add(new Claim(ClaimTypes.Role, role));
            }

            return usersClaims;
        }

        public bool Equals(FantasyCriticUser other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return UserID.Equals(other.UserID);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((FantasyCriticUser) obj);
        }

        public override int GetHashCode()
        {
            return UserID.GetHashCode();
        }
    }
}
