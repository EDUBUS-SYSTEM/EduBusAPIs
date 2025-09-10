﻿using Data.Models;
using Data.Repos.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repos.SqlServer
{
    public class StudentRepository : SqlRepository<Student>, IStudentRepository
    {
        public StudentRepository(DbContext dbContext) : base(dbContext)
        {
        }
        public async Task<List<Student>> GetStudentsByParentEmailAsync(string email)
        {
            return await _table.Where(s => s.ParentEmail == email && !s.IsDeleted)
                               .ToListAsync();
        }
    }
}
