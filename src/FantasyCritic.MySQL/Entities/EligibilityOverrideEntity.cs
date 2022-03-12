﻿using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using FantasyCritic.Lib.Domain;
using Newtonsoft.Json;

namespace FantasyCritic.MySQL.Entities
{
    internal class EligibilityOverrideEntity
    {
        public EligibilityOverrideEntity()
        {

        }

        public EligibilityOverrideEntity(League league, int year, EligibilityOverride domain)
        {
            LeagueID = league.LeagueID;
            Year = year;
            MasterGameID = domain.MasterGame.MasterGameID;
            Eligible = domain.Eligible;
        }

        public Guid LeagueID { get; set; }
        public int Year { get; set; }
        public Guid MasterGameID { get; set; }
        public bool Eligible { get; set; }

        public EligibilityOverride ToDomain(MasterGame masterGame)
        {
            return new EligibilityOverride(masterGame, Eligible);
        }
    }
}