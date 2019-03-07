using System;
using System.Text.RegularExpressions;
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

        [HttpGet("{userId}", Name = nameof(GetUserById))]

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
        public IActionResult CreateUser([FromBody] NewUserDto user)
        {
            if (user == null)
                return BadRequest();
            var createdUserEntity = Mapper.Map<UserEntity>(user);
            if (!Regex.IsMatch(user.Login, @"^[a-zA-Z0-9]+$"))
                ModelState.AddModelError("Login", "Неправильный логин");
            if (!ModelState.IsValid)
                return new UnprocessableEntityObjectResult(ModelState);
            createdUserEntity = Repository.Insert(createdUserEntity);
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = createdUserEntity.Id },
                createdUserEntity.Id);
        }

        [HttpDelete("{userId}")]
        public ActionResult<UserDto> DeleteUser([FromRoute] Guid userId)
        {
            var user = Repository.FindById(userId);
            Repository.Delete(userId);
            if (user == null)
                return NotFound();
            return NoContent();
        }

    }
}