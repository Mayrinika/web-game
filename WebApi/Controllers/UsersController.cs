using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;
using WebApi.Models;
using WebGame.Domain;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    public class UsersController : Controller
    {
        private readonly IUserRepository userRepository;
        private readonly LinkGenerator linkGenerator;

        public UsersController(
            IUserRepository userRepository,
            LinkGenerator linkGenerator)
        {
            this.userRepository = userRepository;
            this.linkGenerator = linkGenerator;
        }

        /// <summary>
        /// Получить пользователя
        /// </summary>
        /// <param name="userId">Идентификатор пользователя</param>
        [HttpGet("{userId}", Name = nameof(GetUserById))]
        [HttpHead("{userId}")]
        [Produces("application/json", "application/xml")]
        [SwaggerResponse(200, "OK", typeof(UserDto))]
        [SwaggerResponse(404, "Пользователь не найден")]
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            var userFromRepo = userRepository.FindById(userId);
            if (userFromRepo == null)
                return NotFound();

            var userDto = Mapper.Map<UserDto>(userFromRepo);
            return Ok(userDto);
        }

        /// <summary>
        /// Создать пользователя
        /// </summary>
        /// <remarks>
        /// Пример запроса:
        ///
        ///     POST /api/users
        ///     {
        ///        "login": "johndoe375",
        ///        "firstName": "John",
        ///        "lastName": "Doe"
        ///     }
        ///
        /// </remarks>
        /// <param name="user">Данные для создания пользователя</param>
        [HttpPost]
        [Consumes("application/json")]
        [Produces("application/json", "application/xml")]
        [SwaggerResponse(201, "Пользователь создан")]
        [SwaggerResponse(400, "Некорректные входные данные")]
        [SwaggerResponse(422, "Ошибка при проверке")]
        public IActionResult CreateUser([FromBody] UserToCreateDto user)
        {
            if (user == null)
                return BadRequest();

            if (!string.IsNullOrEmpty(user.Login) &&
                !user.Login.All(c => char.IsLetter(c) || char.IsDigit(c)))
            {
                ModelState.AddModelError(nameof(UserToCreateDto.Login),
                    "Login should contain only letters or digits.");
            }

            if (!ModelState.IsValid)
                return new UnprocessableEntityObjectResult(ModelState);

            var userEntity = Mapper.Map<UserEntity>(user);
            var createdUserEntity = userRepository.Insert(userEntity);

            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = createdUserEntity.Id },
                createdUserEntity.Id);
        }

        /// <summary>
        /// Удалить пользователя
        /// </summary>
        /// <param name="userId">Идентификатор пользователя</param>
        [HttpDelete("{userId}")]
        [Produces("application/json", "application/xml")]
        [SwaggerResponse(204, "Пользователь удален")]
        [SwaggerResponse(404, "Пользователь не найден")]
        public IActionResult DeleteUser([FromRoute] Guid userId)
        {
            var userFromRepo = userRepository.FindById(userId);
            if (userFromRepo == null)
                return NotFound();

            userRepository.Delete(userId);
            return NoContent();
        }

        /// <summary>
        /// Обновить пользователя
        /// </summary>
        /// <param name="userId">Идентификатор пользователя</param>
        /// <param name="user">Обновленные данные пользователя</param>
        [HttpPut("{userId}")]
        [Consumes("application/json")]
        [Produces("application/json", "application/xml")]
        [SwaggerResponse(201, "Пользователь создан")]
        [SwaggerResponse(204, "Пользователь обновлен")]
        [SwaggerResponse(400, "Некорректные входные данные")]
        [SwaggerResponse(422, "Ошибка при проверке")]
        public IActionResult UpdateUser([FromRoute] Guid userId,
            [FromBody] UserToUpdateDto user)
        {
            if (user == null || userId == Guid.Empty)
                return BadRequest();

            ValidateUserToUpdate(user);
            if (!ModelState.IsValid)
                return new UnprocessableEntityObjectResult(ModelState);

            var userFromRepo = userRepository.FindById(userId);
            if (userFromRepo == null)
            {
                var newUserEntity = new UserEntity(userId);
                Mapper.Map(user, newUserEntity);
                userRepository.UpdateOrInsert(newUserEntity);

                return CreatedAtRoute(
                    nameof(GetUserById),
                    new { userId = newUserEntity.Id },
                    newUserEntity.Id);
            }

            Mapper.Map(user, userFromRepo);
            userRepository.Update(userFromRepo);
            return NoContent();
        }

        private void ValidateUserToUpdate(UserToUpdateDto user)
        {
            if (!string.IsNullOrEmpty(user.Login) && !user.Login.All(c => char.IsLetter(c) || char.IsDigit(c)))
            {
                ModelState.AddModelError(nameof(UserToUpdateDto.Login),
                    "Login should contain only letters or digits.");
            }
        }

        /// <summary>
        /// Частично обновить пользователя
        /// </summary>
        /// <param name="userId">Идентификатор пользователя</param>
        /// <param name="patchDoc">JSON Patch для пользователя</param>
        [HttpPatch("{userId}")]
        [Consumes("application/json-patch+json")]
        [Produces("application/json", "application/xml")]
        [SwaggerResponse(204, "Пользователь обновлен")]
        [SwaggerResponse(400, "Некорректные входные данные")]
        [SwaggerResponse(404, "Пользователь не найден")]
        [SwaggerResponse(422, "Ошибка при проверке")]
        public IActionResult PartiallyUpdateUser([FromRoute] Guid userId,
            [FromBody] JsonPatchDocument<UserToUpdateDto> patchDoc)
        {
            if (patchDoc == null)
                return BadRequest();

            var userFromRepo = userRepository.FindById(userId);
            if (userFromRepo == null)
                return NotFound();

            var user = Mapper.Map<UserToUpdateDto>(userFromRepo);

            patchDoc.ApplyTo(user, ModelState);
            TryValidateModel(user);
            ValidateUserToUpdate(user);
            if (!ModelState.IsValid)
                return new UnprocessableEntityObjectResult(ModelState);

            Mapper.Map(user, userFromRepo);
            userRepository.Update(userFromRepo);

            return NoContent();
        }

        /// <summary>
        /// Получить пользователей
        /// </summary>
        /// <param name="pageNumber">Номер страницы, по умолчанию 1</param>
        /// <param name="pageSize">Размер страницы, по умолчанию 20</param>
        /// <response code="200">OK</response>
        [HttpGet(Name = nameof(GetUsers))]
        [Produces("application/json", "application/xml")]
        [ProducesResponseType(typeof(IEnumerable<UserDto>), 200)]
        public IActionResult GetUsers(int pageNumber = 1, int pageSize = 10)
        {
            pageNumber = Math.Max(pageNumber, 1);
            pageSize = Math.Min(Math.Max(pageSize, 1), 20);
            var pageList = userRepository.GetPage(pageNumber, pageSize);

            var users = Mapper.Map<IEnumerable<UserDto>>(pageList);

            var paginationHeader = new
            {
                previousPageLink = pageList.HasPrevious
                    ? CreateGetUsersUri(pageList.CurrentPage - 1, pageList.PageSize)
                    : null,
                nextPageLink = pageList.HasNext
                    ? CreateGetUsersUri(pageList.CurrentPage + 1, pageList.PageSize)
                    : null,
                totalCount = pageList.TotalCount,
                pageSize = pageList.PageSize,
                currentPage = pageList.CurrentPage,
                totalPages = pageList.TotalPages
            };
            Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(paginationHeader));

            return Ok(users);
        }

        private string CreateGetUsersUri(int pageNumber, int pageSize)
        {
            return linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers),
                new
                {
                    pageNumber,
                    pageSize
                });
        }

        /// <summary>
        /// Опции по запросам о пользователях
        /// </summary>
        [HttpOptions]
        [SwaggerResponse(200, "OK")]
        public IActionResult GetUsersOptions()
        {
            Response.Headers.Add("Allow", "POST,GET,OPTIONS");
            return Ok();
        }
    }
}