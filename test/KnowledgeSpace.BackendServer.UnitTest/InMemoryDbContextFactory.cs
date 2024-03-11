﻿using KnowledgeSpace.BackendServer.Data;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeSpace.BackendServer.UnitTest
{
    public class InMemoryDbContextFactory
    {
        public ApplicationDbContext GetApplicationDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "InMemoryApplicationDatabase")
                .Options;

            var dbContext = new ApplicationDbContext(options);

            return dbContext;
        }
    }
}
