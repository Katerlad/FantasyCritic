﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FantasyCritic.Lib.Domain;
using NodaTime;

namespace FantasyCritic.MySQL.Entities
{
    public class QueuedGameEntity
    {
        public QueuedGameEntity()
        {
            
        }

        public QueuedGameEntity(QueuedGame domain)
        {
            PublisherID = domain.Publisher.PublisherID;
            MasterGameID = domain.MasterGame.MasterGameID;
            Rank = domain.Rank;
        }

        public Guid PublisherID { get; set; }
        public Guid MasterGameID { get; set; }
        public int Rank { get; set; }

        public QueuedGame ToDomain(Publisher publisher, MasterGame masterGame)
        {
            return new QueuedGame(publisher, masterGame, Rank);
        }
    }
}
