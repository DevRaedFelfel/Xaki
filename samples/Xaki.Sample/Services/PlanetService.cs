﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xaki.Sample.Models;

namespace Xaki.Sample.Services
{
    public class PlanetService
    {
        private readonly DataContext _dc;

        public PlanetService(DataContext dc)
        {
            _dc = dc;
        }

        public async Task<IEnumerable<Planet>> GetPlanets()
        {
            return await _dc.Planets.ToListAsync();
        }

        public async Task<Planet> GetPlanetById(int planetId)
        {
            return await _dc.Planets.SingleOrDefaultAsync(i => i.PlanetId == planetId);
        }
    }
}
