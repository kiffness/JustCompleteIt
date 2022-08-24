using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using API.Entities;
using API.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace API.Services
{
    public class TokenService : ITokenService
    {
        private readonly SymmetricSecurityKey _key;
        public TokenService(IConfiguration config)
        {   
            //Create a new key
            _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["TokenKey"]));
        }
        public string CreateToken(AppUser user)
        {   
            //Our Claims
            var claims = new List<Claim>
            {   
                //use the NameId to store the username this will be our name identifier for everything
                new Claim(JwtRegisteredClaimNames.NameId, user.UserName)
            };

            //Create some creds so we save our creds = the new signing credentials
            var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha512Signature);

            //Describe our token
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                //claims
                Subject = new ClaimsIdentity(claims),
                //Expires
                Expires = DateTime.Now.AddDays(7),
                //Signing Creds
                SigningCredentials = creds
            };

            //Create an instance of tokenhandler
            var tokenHandler = new JwtSecurityTokenHandler();
            //Create a token
            var token = tokenHandler.CreateToken(tokenDescriptor);
            //return the created token to string
            return tokenHandler.WriteToken(token);
        }
    }
}