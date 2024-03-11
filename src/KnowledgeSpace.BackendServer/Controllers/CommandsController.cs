﻿using KnowledgeSpace.BackendServer.Data;
using KnowledgeSpace.ViewModels.Systems;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeSpace.BackendServer.Controllers
{
    public class CommandsController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public CommandsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetCommands()
        {
            var commands = _context.Commands;

            var commandVms = await commands.Select(f => new CommandVm()
            {
                Id = f.Id,
                Name = f.Name,
            }).ToListAsync();

            return Ok(commandVms);
        }

    }
}
