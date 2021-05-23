﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Contracts.BLL.Base.Services;
using Contracts.DAL.App.Repositories;
using BLLAppDTO = BLL.App.DTO;
using DALAppDTO = DAL.App.DTO;

namespace Contracts.BLL.App.Services
{
    public interface IGameTeamService : IBaseEntityService<BLLAppDTO.GameTeam, DALAppDTO.GameTeam>, IGameTeamRepositoryCustom<BLLAppDTO.GameTeam>
    {
        public new Task<IEnumerable<BLLAppDTO.GameTeam>> GetAllTeamGamesWithGameIdAsync(Guid gameId,  bool noTracking = true);
        public Task RemoveGamesWithGameIdAsync(Guid id,  bool noTracking = true);
        public Task RemoveWithGameIdAsync(Guid id);
    }
}