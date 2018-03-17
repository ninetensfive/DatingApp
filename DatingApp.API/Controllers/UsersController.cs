using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using DatingApp.API.Data.Contracts;
using DatingApp.API.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DatingApp.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class UsersController : Controller
    {
        private readonly IDatingRepository datingRepository;
        private readonly IMapper mapper;
        public UsersController(IDatingRepository datingRepository, IMapper mapper)
        {
            this.mapper = mapper;
            this.datingRepository = datingRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = await this.datingRepository.GetUsers();
            var usersToReturn = this.mapper.Map<IEnumerable<UserForListDTO>>(users);

            return Ok(usersToReturn);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int Id)
        {
            var user = await this.datingRepository.GetUser(Id);
            var userToreturn = this.mapper.Map<UserForDetailedDTO>(user);

            return Ok(userToreturn);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateUser(int id, [FromBody] UserForUpdateDTO userForUpdateDTO){
            if(!ModelState.IsValid){
                return BadRequest(ModelState);
            }

            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var userFromRepo = await this.datingRepository.GetUser(id);      

            if(userFromRepo == null)
            {
                return NotFound($"Could not find user with an ID of {id}");
            }

            if(currentUserId != userFromRepo.Id){
                return Unauthorized();
            }

            this.mapper.Map(userForUpdateDTO, userFromRepo);

            if(await this.datingRepository.SaveAll()){
                return NoContent();
            }

            throw new Exception($"Updating user {id} failed on save");
        }

    }
}