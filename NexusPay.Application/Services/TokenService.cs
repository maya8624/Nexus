using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using NexusPay.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace NexusPay.Application.Services
{
    public class TokenService(IConfiguration config) : ITokenService
    {
        public string CreateToken(string userId, string email)
        {
            // 1. Define the User's "Claims" (The data inside the token)
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, userId),
                new(JwtRegisteredClaimNames.Email, email),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // Unique ID for this specific token
            };

            // 2. Setup the Key and Credentials
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

            // 3. Create the Token Descriptor
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(2),
                SigningCredentials = creds,
                Issuer = config["Jwt:Issuer"],  
                Audience = config["Jwt:Audience"]
            };

            // 4. Generate the String
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}
