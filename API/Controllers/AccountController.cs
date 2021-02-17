using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AccountController : BaseApiController
    {
        private readonly DataContext _context;
        private readonly ITokenService _tokenService;

        public AccountController(DataContext context, ITokenService tokenService)
        {
            this._context = context;
            this._tokenService = tokenService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDTO>> Register(RegisterDto registerDto)
        {

            if (await userExists(registerDto.UserName)) return BadRequest("username is already taken");

            using var hmac = new HMACSHA512();

            var user = new AppUser()
            {
                UserName = registerDto.UserName.ToLower(),
                PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.PassWord)),
                PasswordSalt = hmac.Key
            };

            await _context.User.AddAsync(user);
            await _context.SaveChangesAsync();

            return new UserDTO() { Username = user.UserName, Token = _tokenService.CreateToken(user) };
        }

        [HttpPost("Login")]
        public async Task<ActionResult<UserDTO>> Login(LoginDto loginDto)
        {

            var user = await _context.User.SingleOrDefaultAsync(us => us.UserName == loginDto.Username.ToLower());
            if (user == null) return Unauthorized("invalid username");

            using var hmac = new HMACSHA512(user.PasswordSalt);
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));

            for (var i = 0; i < user.PasswordHash.Length; i++)
            {
                if (user.PasswordHash[i] != computedHash[i]) return Unauthorized("invalid password");
            }

            return new UserDTO() { Username = user.UserName, Token = _tokenService.CreateToken(user) };

        }


        private async Task<bool> userExists(string username)
        {
            return await _context.User.AnyAsync(u => u.UserName == username.ToLower());
        }
    }
}
