﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FantasyCritic.Lib.Domain
{
    public class SiteCounts
    {
        public SiteCounts(int userCount, int leagueCount, int masterGameCount, int publisherGameCount)
        {
            UserCount = userCount;
            LeagueCount = leagueCount;
            MasterGameCount = masterGameCount;
            PublisherGameCount = publisherGameCount;
        }

        public int UserCount { get; }
        public int LeagueCount { get; }
        public int MasterGameCount { get; }
        public int PublisherGameCount { get; }
    }
}
