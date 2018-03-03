using System.Threading.Tasks;
using DatingApp.API.Data.Contracts;
using DatingApp.API.DTOs;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Mvc;

namespace DatingApp.API.Controllers
{
    [Route("api/[controller]")]
    public class AuthController : Controller
    {
        private readonly IAuthRepository repository;
        public AuthController(IAuthRepository repository)
        {
            this.repository = repository;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody]UserRegisterDTO userRegisterDTO)
        {
             userRegisterDTO.UserName = userRegisterDTO.UserName.ToLower();

            if(await this.repository.UserExists(userRegisterDTO.UserName))
            {
               ModelState.AddModelError("Username", "Username already exists");
            }

            if(!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var newUser = new User{
                UserName = userRegisterDTO.UserName
            };

            var createUser = await this.repository.Register(newUser, userRegisterDTO.Password);

            return StatusCode(201);
        }
    }
}