using Grab.Core.Interfaces;
using Grab.Core.Models;
using Grab.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grab.Infrastructure.Repositories
{
    public class DirectoryRepository : IDirectoryRepository
    {
        private readonly GrabDbContext _context;

        public DirectoryRepository(GrabDbContext context)
        {
            _context = context;
        }

        public async Task<Directory?> GetByPathAsync(string path)
        {
            return await _context.Directories.FindAsync(path);
        }

        public async Task<IEnumerable<Directory>> GetAllAsync()
        {
            return await _context.Directories.ToListAsync();
        }

        public async Task<IEnumerable<Directory>> GetByStatusAsync(DirectoryStatus status)
        {
            return await _context.Directories
                .Where(d => d.Status == status)
                .ToListAsync();
        }

        public async Task<bool> AddAsync(Directory directory)
        {
            _context.Directories.Add(directory);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateAsync(Directory directory)
        {
            _context.Directories.Update(directory);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(string path)
        {
            var directory = await _context.Directories.FindAsync(path);
            if (directory == null)
                return false;

            _context.Directories.Remove(directory);
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
