using Grab.Core.Interfaces;
using Grab.Core.Models;
using Grab.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grab.Infrastructure.Repositories
{
    public class ExtractedDataRepository : IExtractedDataRepository
    {
        private readonly GrabDbContext _context;

        public ExtractedDataRepository(GrabDbContext context)
        {
            _context = context;
        }

        public async Task<ExtractedData?> GetByIdAsync(int id)
        {
            return await _context.ExtractedData.FindAsync(id);
        }

        public async Task<IEnumerable<ExtractedData>> GetByFilePathAsync(string filePath)
        {
            return await _context.ExtractedData
                .Where(d => d.FilePath == filePath)
                .ToListAsync();
        }

        public async Task<IEnumerable<ExtractedData>> GetByTaskIdAsync(int taskId)
        {
            return await _context.ExtractedData
                .Where(d => d.TaskId == taskId)
                .ToListAsync();
        }

        public async Task<bool> AddAsync(ExtractedData data)
        {
            _context.ExtractedData.Add(data);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateAsync(ExtractedData data)
        {
            _context.ExtractedData.Update(data);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var data = await _context.ExtractedData.FindAsync(id);
            if (data == null)
                return false;

            _context.ExtractedData.Remove(data);
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
