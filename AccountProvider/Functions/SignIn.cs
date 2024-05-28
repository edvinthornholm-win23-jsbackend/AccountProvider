using AccountProvider.Models;
using Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;

namespace AccountProvider.Functions
{
    public class SignIn
    {
        private readonly ILogger<SignIn> _logger;
        private readonly UserManager<UserAccount> _userManager;
        private readonly SignInManager<UserAccount> _signInManager;

        public SignIn(ILogger<SignIn> logger, SignInManager<UserAccount> signInManager, UserManager<UserAccount> userManager)
        {
            _logger = logger;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [Function("SignIn")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            string body = null!;

            try
            {
                body = await new StreamReader(req.Body).ReadToEndAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"StreamReader :: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            if (!string.IsNullOrEmpty(body))
            {
                UserLoginRequest ulr = null!;

                try
                {
                    ulr = JsonConvert.DeserializeObject<UserLoginRequest>(body)!;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"JsonConvert.DeserializeObject<UserLoginRequest> :: {ex.Message}");
                    return new BadRequestObjectResult("Invalid request body.");
                }

                if (ulr != null && !string.IsNullOrEmpty(ulr.Email) && !string.IsNullOrEmpty(ulr.Password))
                {
                    try
                    {
                        var userAccount = await _userManager.FindByEmailAsync(ulr.Email);
                        if (userAccount == null)
                        {
                            return new UnauthorizedResult();
                        }

                        var result = await _signInManager.CheckPasswordSignInAsync(userAccount, ulr.Password, false);
                        if (result.Succeeded)
                        {
                            var token = GenerateJwtToken(userAccount);
                            return new OkObjectResult(new { AccessToken = token });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"CheckPasswordSignInAsync :: {ex.Message}");
                        return new StatusCodeResult(StatusCodes.Status500InternalServerError);
                    }

                    return new UnauthorizedResult();
                }

                return new BadRequestObjectResult("Email and password must be provided.");
            }

            return new BadRequestObjectResult("Request body cannot be empty.");
        }

        private string GenerateJwtToken(UserAccount user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(Environment.GetEnvironmentVariable("JWT_SECRET"));
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()) // Add user ID claim
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token); // decode: https://jwt.io/
        }
    }
}



//using AccountProvider.Models;
//using Data.Entities;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Azure.Functions.Worker;
//using Microsoft.Extensions.Logging;
//using Newtonsoft.Json;
//using System;

//namespace AccountProvider.Functions
//{
//    public class SignIn(ILogger<SignIn> logger, SignInManager<UserAccount> signInManager, UserManager<UserAccount> userManager)
//    {
//        private readonly ILogger<SignIn> _logger = logger;
//        private readonly UserManager<UserAccount> _userManager = userManager;
//        private readonly SignInManager<UserAccount> _signInManager = signInManager;

//        [Function("SignIn")]
//        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
//        {
//            string body = null!;

//            try
//            {
//                body = await new StreamReader(req.Body).ReadToEndAsync();
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError($"StreamReader :: {ex.Message}");
//            }

//            if (body != null)
//            {

//                UserLoginRequest ulr = null!;

//                try
//                {
//                    ulr = JsonConvert.DeserializeObject<UserLoginRequest>(body)!;
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError($"JsonConvert.DeserializeObject<UserLoginRequest> :: {ex.Message}");
//                }

//                if (ulr != null && ulr.Email != null && ulr.Password != null)
//                {
//                    try
//                    {
//                        var userAccount = await _userManager.FindByEmailAsync(ulr.Email);
//                        var result = await _signInManager.CheckPasswordSignInAsync(userAccount!, ulr.Password, false);
//                        if (result.Succeeded)
//                        {
//                            // Get accestoken from token provider
//                            return new OkObjectResult("accesstoken");
//                        }


//                    }
//                    catch (Exception ex)
//                    {
//                        _logger.LogError($"_signInManager.PasswordSignInAsync :: {ex.Message}");
//                    }

//                    return new UnauthorizedResult();
//                }

//            }


//            return new BadRequestResult();
//        }
//    }
//}
