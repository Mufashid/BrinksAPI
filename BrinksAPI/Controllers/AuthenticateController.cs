using BrinksAPI.Auth;
using BrinksAPI.Entities;
using BrinksAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BrinksAPI.Controllers
{

    [Route("api/")]
    [ApiController]
    public class AuthenticateController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AuthenticateController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        #region User Register

        [HttpPost]
        [Route("register/user")]
        [Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> Register([FromBody] Register model)
        {
            if(!ModelState.IsValid)
                return BadRequest(model);
            var userExists = _context.users.Where(user=>user.Email == model.Email).Any();
            if (userExists)
                return Conflict(new Response { Status = "Error", Message = "User already exists!" });

            User user = new()
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email=  model.Email,
                Password   = model.Password,
                isActive    =   true,
                AuthLevelRefId=2,
                CreatedTime  = DateTime.UtcNow,
                UpdatedTime= DateTime.UtcNow,
            };
            var result = _context.users.Add(user);
            
            _context.SaveChanges();
            if (result == null)
                return StatusCode(StatusCodes.Status500InternalServerError, new Response { Status = "Error", Message = "User creation failed! Please check user details and try again." });

            return Ok(new Response { Status = "Success", Message = "User created successfully!" });
        }
        #endregion

        #region Admin Register
        [HttpPost]
        [Route("register/admin")]
        [Authorize(Roles =UserRoles.Admin)]
        public async Task<IActionResult> RegisterAdmin([FromBody] Register model)
        {
            if (!ModelState.IsValid)
                return BadRequest(model);

            var userExists = _context.users.Where(user => user.Email == model.Email).Any();

            if (userExists)
                return Conflict(new Response { Status = "Error", Message = "Admin already exists!" });

            User user = new()
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                Password = model.Password,
                isActive = true,
                AuthLevelRefId = 1,
                CreatedTime = DateTime.UtcNow,
                UpdatedTime = DateTime.UtcNow,
            };
            var result = _context.users.Add(user);

            _context.SaveChanges();
            if (result == null)
                return StatusCode(StatusCodes.Status500InternalServerError, new Response { Status = "Error", Message = "User creation failed! Please check user details and try again." });

            return Ok(new Response { Status = "Success", Message = "Admin created successfully!" });
        }
        #endregion
    }
}

