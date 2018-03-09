using System.Collections.Generic;
using System.Threading.Tasks;
using DatingApp.API.Data.Contracts;
using DatingApp.API.Models;
using Microsoft.EntityFrameworkCore;

namespace DatingApp.API.Data
{
    public class DatingRepository : IDatingRepository
    {
        private readonly DataContext context;
        public DatingRepository(DataContext context)
        {
            this.context = context;
        }
        public void Add<T>(T entity) where T : class
        {
            this.context.Add(entity);
        }

        public void Delete<T>(T entity) where T : class
        {
            this.context.Remove(entity);
        }

        public async Task<User> GetUser(int Id)
        {
            var user = await this.context
                .Users
                .Include(u => u.Photos)
                .SingleOrDefaultAsync(u=> u.Id == Id);
            
            return user;
        }

        public async Task<IEnumerable<User>> GetUsers()
        {
            var users = await this.context
                .Users
                .Include(u => u.Photos)
                .ToListAsync();
            
            return users;
        }

        public async Task<bool> SaveAll()
        {
            return await this.context.SaveChangesAsync() > 0;
        }
    }
}