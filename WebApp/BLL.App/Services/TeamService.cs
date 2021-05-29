﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using BLL.App.Mappers;
using BLL.Base.Services;
using Contracts.BLL.App.Services;
using Contracts.DAL.App;
using Contracts.DAL.App.Repositories;
using BLLAppDTO = BLL.App.DTO;
using DALAppDTO = DAL.App.DTO;

namespace BLL.App.Services
{
    public class TeamService: BaseEntityService<IAppUnitOfWork, ITeamRepository, BLLAppDTO.Team, DALAppDTO.Team>, ITeamService
    {
        public TeamService(IAppUnitOfWork serviceUow, ITeamRepository serviceRepository, IMapper mapper) : base(serviceUow, serviceRepository, new TeamMapper(mapper))
        {
        }

        public async Task<BLLAppDTO.ClientTeam> GetClientTeamAsync(Guid teamId,  IMapper mapper)
        {
            var team = Mapper.Map(await ServiceRepository.FirstOrDefaultAsync(teamId));

            var staffList = new List<BLLAppDTO.TeamPerson>();
            var playerList = new List<BLLAppDTO.TeamPerson>();

            foreach (var person in team!.TeamPersons!)
            {

                var date = person.Person.Date.ToShortDateString();

                var newDAte = DateTime.Parse(date);
                
                if (person.Role.Name == "Player" || person.Role.Name == "Mängija")
                {
                    playerList.Add(person);
                }
                else
                {
                    staffList.Add(person);
                }
            }

            var clubMapper = new ClubMapper(mapper);
            
            var club = clubMapper.Map((await ServiceUow.ClubTeams.GetClubWithTeamIdAsync(teamId)).Club);

            var clientTeam = new BLLAppDTO.ClientTeam
            {
                Team = team!,
                Club = club!,
                StaffList = staffList,
                PlayerList = playerList

            };

            return clientTeam;
        }
    }
}