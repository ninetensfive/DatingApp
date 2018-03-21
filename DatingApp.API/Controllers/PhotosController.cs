using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DatingApp.API.Data.Contracts;
using DatingApp.API.DTOs;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DatingApp.API.Controllers
{
    [Authorize]
    [Route("api/users/{userId}/photos")]
    public class PhotosController : Controller
    {
        private readonly IDatingRepository datingRepository;
        private readonly IMapper mapper;
        private readonly IOptions<CloudinarySettings> cloudinaryConfig;
        private Cloudinary cloudinary;

        public PhotosController(IDatingRepository datingRepository,
        IMapper mapper, IOptions<CloudinarySettings> cloudinaryConfig)
        {
            this.cloudinaryConfig = cloudinaryConfig;
            this.mapper = mapper;
            this.datingRepository = datingRepository;

            Account acc = new Account(
                this.cloudinaryConfig.Value.CloudName,
                this.cloudinaryConfig.Value.ApiKey,
                this.cloudinaryConfig.Value.ApiSecret
            );

            this.cloudinary = new Cloudinary(acc);
        }

        [HttpPost]
        public async Task<IActionResult> AddPhotoForUser(int userId, PhotoForCreatingDTO photoDTO)
        {
            var user = await this.datingRepository.GetUser(userId);

            if (user == null)
            {
                return BadRequest("Could not find user");
            }

            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            if (user.Id != currentUserId)
            {
                return Unauthorized();
            }

            var file = photoDTO.File;
            var uploadResult = new ImageUploadResult();

            if (file.Length > 0)
            {
                using (var stream = file.OpenReadStream())
                {
                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(file.Name, stream),
                        Transformation = new Transformation().Width(1000).Height(1000).Crop("fill").Gravity("face")
                    };

                    uploadResult = this.cloudinary.Upload(uploadParams);
                }
            }

            photoDTO.Url = uploadResult.Uri.ToString();
            photoDTO.PublicId = uploadResult.PublicId;

            var photo = this.mapper.Map<Photo>(photoDTO);
            photo.User = user;

            if(!user.Photos.Any(m => m.IsMain)){
                photo.IsMain = true;
            }

            user.Photos.Add(photo);

            if(await this.datingRepository.SaveAll()){
                var photoForReturn = this.mapper.Map<PhotoForReturnDTO>(photo);
                return CreatedAtRoute("GetPhoto", new {id = photo.Id}, photoForReturn);
            }

            return BadRequest("Could not add the photo");
        }

        [HttpGet("{id}", Name = "GetPhoto")]
        public async Task<IActionResult> GetPhoto(int id){
            var photoFromRepo = await this.datingRepository.GetPhoto(id);

            var photo = this.mapper.Map<PhotoForReturnDTO>(photoFromRepo);

            return Ok(photo);
        }

        [HttpPost("{id}/setMain")]
        public async Task<IActionResult> SetMainPhoto(int userId, int id){
            if(userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value)){
                return Unauthorized();
            }

            var photoFromRepo = await this.datingRepository.GetPhoto(id);

            if(photoFromRepo == null){
                return NotFound();
            }

            if(photoFromRepo.IsMain){
                return BadRequest("This is already the main photo");
            }

            var currentMainPhoto = await this.datingRepository.GetMainPhotoForUser(userId);

            if(currentMainPhoto != null){
                currentMainPhoto.IsMain = false;
            }

            photoFromRepo.IsMain = true;

            if(await this.datingRepository.SaveAll()){
                return NoContent();
            }

            return BadRequest("Could not set photo to main");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePhoto(int userId, int id){
            if(userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value)){
                return Unauthorized();
            }

            var photoFromRepo = await this.datingRepository.GetPhoto(id);

            if(photoFromRepo == null){
                return NotFound();
            }

            if(photoFromRepo.IsMain){
                return BadRequest("You cannot delete the main photo");
            }

            if(photoFromRepo.PublicId != null){
                var deleteParams = new DeletionParams(photoFromRepo.PublicId);
                var result = this.cloudinary.Destroy(deleteParams);                      
            }

             this.datingRepository.Delete(photoFromRepo);

            if(await this.datingRepository.SaveAll()){
                return Ok();
            }

            return BadRequest("Failed to delete the photo");
        }

    }
}