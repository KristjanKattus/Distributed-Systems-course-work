﻿using System;
using Domain.App;
using Domain.Base;

namespace PublicApi.DTO.v1
{
    public class GameTeam
    {
        public Guid Id { get; set; }
        public Guid GameId { get; set; } 
        public Game? Game { get; set; }

        public Guid TeamId { get; set; }
        public Team? Team { get; set; }

        public DateTime Since { get; set; } = DateTime.Now;
        public DateTime? Until { get; set; }
        
        public bool Hometeam { get; set; }
        public int Scored { get; set; } 
        public int Conceded { get; set; }
        public int Points { get; set; }



    }
}