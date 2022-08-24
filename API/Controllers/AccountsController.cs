using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API.Controllers.DTOs;
using API.Data;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountsController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly ITokenService _tokenService;
        public AccountsController(DataContext context, ITokenService tokenService)
        {
            _tokenService = tokenService;
            _context = context;
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto) //Take in a register DTO of username and password, so that it can be passed in as json in the body
        {   
            //Check if the username already exists
            if(await UserExists(registerDto.Username.ToLower())) return BadRequest("Username is taken");

            using var hmac = new HMACSHA512(); //The cryptography type used

            //Create a new AppUser Object
            var user = new AppUser
            {   
                //UserName is the username got from frontend form or api request
                UserName = registerDto.Username.ToLower(),
                //hash the password
                PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password)),
                //Salt the password
                PasswordSalt = hmac.Key
            };
            
            //Save the new user in the database
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            //Return the api response as the user
            return new UserDto
            {
                UserName = user.UserName,
                Token = _tokenService.CreateToken(user)
            };
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
        {
            //Get the user from the db using the given username
            var user = await _context.Users.SingleOrDefaultAsync(x => x.UserName == loginDto.Username.ToLower());

            //If the user object returned from db, it means it does not exist
            if (user == null) return Unauthorized("Invalid username or password");

            //new HMAC class using the users password salt in the db
            using var hmac = new HMACSHA512(user.PasswordSalt);

            //use hmac to generate a new hash using the password salt
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));

            //Check each character of the new computed hash matches the saved password hash
            for (int i = 0; i < computedHash.Length; i++)
            {
                if(computedHash[i] != user.PasswordHash[i]) return Unauthorized("Invalid username or password");
            }

            //Return our user object
            return new UserDto
            {
                UserName = user.UserName,
                Token = _tokenService.CreateToken(user)
            };
        }

        private async Task<bool> UserExists(string username)
        {
            return await _context.Users.AnyAsync(user => user.UserName == username.ToLower());
        }
    }
}