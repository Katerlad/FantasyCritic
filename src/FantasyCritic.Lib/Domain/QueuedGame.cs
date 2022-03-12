﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NodaTime;

namespace FantasyCritic.Lib.Domain
{
    public class QueuedGame
    {
        public QueuedGame(Publisher publisher, MasterGame masterGame, int rank)
        {
            Publisher = publisher;
            MasterGame = masterGame;
            Rank = rank;
        }

        public Publisher Publisher { get; }
        public MasterGame MasterGame { get; }
        public int Rank { get; }

        public override string ToString()
        {
            return $"{Publisher.PublisherName}|{MasterGame.GameName}|{Rank}";
        }
    }
}
