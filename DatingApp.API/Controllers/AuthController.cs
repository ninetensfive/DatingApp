using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using DatingApp.API.Data.Contracts;
using DatingApp.API.DTOs;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DatingApp.API.Controllers
{
    [Route("api/[controller]")]
    public class AuthController : Controller
    {
        private readonly IAuthRepository repository;
        private readonly IConfiguration configuration;
        public AuthController(IAuthRepository repository, IConfiguration configuration)
        {
            this.configuration = configuration;
            this.repository = repository;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody]UserRegisterDTO userRegisterDTO)
        {
            userRegisterDTO.UserName = userRegisterDTO.UserName.ToLower();

            if (await this.repository.UserExists(userRegisterDTO.UserName))
            {
                ModelState.AddModelError("Username", "Username already exists");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var newUser = new User
            {
                UserName = userRegisterDTO.UserName
            };

            var createUser = await this.repository.Register(newUser, userRegisterDTO.Password);

            return StatusCode(201);
        }

        [HttpPost("login")]
        public async Task<ActionResult> Login([FromBody]UserLoginDTO userLoginDTO)
        {
            var repositoryUser = await this.repository.Login(userLoginDTO.Username.ToLower(), userLoginDTO.Password);

            if (repositoryUser == null)
            {
                return Unauthorized();
            }

            //generate token 
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(this.configuration.GetSection("AppSettings:Token").Value);

            var tokenDesciptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[] {
                    new Claim(ClaimTypes.NameIdentifier, repositoryUser.Id.ToString()),
                    new Claim(ClaimTypes.Name, repositoryUser.UserName)
                }),
                Expires = DateTime.Now.AddDays(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha512)
            };

            var token = tokenHandler.CreateToken(tokenDesciptor);
            var tokenString = tokenHandler.WriteToken(token);

            return Ok(new { tokenString });
        }
    }
}