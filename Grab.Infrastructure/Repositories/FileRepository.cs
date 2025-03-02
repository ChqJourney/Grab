using Grab.Core.Interfaces;
using Grab.Core.Models;
using Grab.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grab.Infrastructure.Repositories
{
    public class FileRepository : IFileRepository
    {
        private readonly GrabDbContext _context;

        public FileRepository(GrabDbContext context)
        {
            _context = context;
        }

        public async Task<FileItem?> GetByPathAsync(string path)
        {
            return await _context.Files.FindAsync(path);
        }

        public async Task<IEnumerable<FileItem>> GetByDirectoryPathAsync(string directoryPath)
        {
            return await _context.Files
                .Where(f => f.DirectoryPath == directoryPath)
                .ToListAsync();
        }

        public async Task<IEnumerable<FileItem>> GetByStatusAsync(FileStatus status)
        {
            return await _context.Files
                .Where(f => f.Status == status)
                .ToListAsync();
        }

        public async Task<bool> AddAsync(FileItem file)
        {
            _context.Files.Add(file);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateAsync(FileItem file)
        {
            _context.Files.Update(file);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(string path)
        {
            var file = await _context.Files.FindAsync(path);
            if (file == null)
                return false;

            _context.Files.Remove(file);
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
