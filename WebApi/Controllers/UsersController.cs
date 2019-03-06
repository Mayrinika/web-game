using System;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using WebApi.Models;
using WebGame.Domain;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    public class UsersController : Controller
    {
        public IUserRepository Repository { get; }

        public UsersController(IUserRepository repository)
        {
            Repository = repository;
        }

        [HttpGet("{userId}")]
        
        [HttpHead("{userId}")]
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            var user = Repository.FindById(userId);
            if (user == null)
                return NotFound();
            var userDto = Mapper.Map<UserDto>(user);
            return Ok(userDto);
        }

        [HttpPost]
        public IActionResult CreateUser([FromBody] object user)
        {
            throw new NotImplementedException();
        }
    }
}