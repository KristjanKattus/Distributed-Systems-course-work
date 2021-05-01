﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Contracts.DAL.App.Repositories;
using DAL.App.DTO;
using DAL.App.EF.Mappers;
using DAL.Base.EF.Repositories;
using Domain.App;
using Microsoft.EntityFrameworkCore;

namespace DAL.App.EF.Repositories
{
    public class TeamPersonRepository : BaseRepository<DAL.App.DTO.TeamPerson, Domain.App.Team_Person, AppDbContext>, ITeamPersonRepository
    {
        public TeamPersonRepository(AppDbContext dbContext, IMapper mapper) : base(dbContext, new TeamPersonMapper(mapper))
        {
        }

        public override async Task<IEnumerable<TeamPerson>> GetAllAsync(Guid userId, bool noTracking = true)
        {
            var query = InitializeQuery(userId, noTracking);

            var resQuery = query
                .Include(t => t.Person)
                .Include(t => t.Team)
                .Include(t => t.Role)
                .Select(t => Mapper.Map(t));

            var res = await resQuery.ToListAsync();

            return res!;
        }

        public override async Task<TeamPerson?> FirstOrDefaultAsync(Guid id, Guid userId = default, bool noTracking = true)
        {
            var query = InitializeQuery(userId, noTracking);

            var resQuery = query
                .Include(t => t.Person)
                .Include(t => t.Team)
                .Include(t => t.Role);
            
            var res = Mapper.Map(await resQuery.FirstOrDefaultAsync(s => s.Id == id));

            return res;
        }
    }
}