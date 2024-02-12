using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using User.Management.Survice.Models;
using User.Management.Survice.Services;
using UserApi.Models;
using UserApi.Models.Authentication.Login;
using UserApi.Models.Authentication.Signup;

namespace UserApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;

        public AuthenticationController(UserManager<IdentityUser> _userManager , RoleManager<IdentityRole> _roleManager,IConfiguration _configuration , IEmailService _emailService, SignInManager<IdentityUser> _signInManager)
        {
            this._userManager = _userManager;
            this._roleManager = _roleManager;
            this._configuration = _configuration;
            this._emailService = _emailService;
            this._signInManager= _signInManager;
        }
        [HttpPost]
        public async Task<IActionResult> RegistrationUser(RegistrationUser registration , string role)
        {
            // Check user alrady ragister or not 
            var userExist = await _userManager.FindByEmailAsync(registration.Email);
            if(userExist != null)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new Response {Message="User Alrady Exist",Status="Error" });
            }
            // mack users 
            IdentityUser user = new()
            {
                Email = registration.Email,
                UserName = registration.UserName,
                SecurityStamp = Guid.NewGuid().ToString(),
                TwoFactorEnabled = true,
            };

            // mack users is exised 
            if (await _roleManager.RoleExistsAsync(role))
            {
                var result = await _userManager.CreateAsync(user, registration.Password);
                if (!result.Succeeded)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new Response { Message = "User failed to create", Status = "Error" });
                }
                // add role users .....

                await _userManager.AddToRoleAsync(user, role);

                // add token varify thie users....

                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var confirmationLink = Url.Action(nameof(ConfirmEmail),"authentication",new {token, email=user.Email},Request.Scheme);
                var message = new Message(new string[] { user.Email! }, "Confirmation Email Link", confirmationLink!);
                _emailService.SendEmail(message);

                // send Status code 
                return StatusCode(StatusCodes.Status201Created, new Response { Message = $"User created and Email Send to {user.Email} successfully", Status = "Succeeded" });
            }
            else
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new Response { Message = "Role does not exist", Status = "Error" });
            }
        }
        [HttpGet("ConfirmEmail")]
        public async Task<IActionResult> ConfirmEmail(string token,  string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if(user != null)
            {
                var rajult = await _userManager.ConfirmEmailAsync(user, token);
                if(rajult.Succeeded)
                {
                    return StatusCode(StatusCodes.Status200OK, new Response { Message = "Email Varifi Succeefully !", Status = "Succeeded" });
                }
            }
            return StatusCode(StatusCodes.Status500InternalServerError, new Response { Message = "User Dasnot Exists !  ", Status = "Error" });
        }
        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login(Login login)
        {
            // chaking users .... 
            var user = await _userManager.FindByNameAsync(login.UserName);
            //2 factor othentieaction

            if (user.TwoFactorEnabled)
            {
                await _signInManager.SignOutAsync();
                await _signInManager.PasswordSignInAsync(user, login.Password, false,true);
                var token = await _userManager.GenerateTwoFactorTokenAsync(user, "Email");
                var message = new Message(new string[] { user.Email! }, "Confirmation OTP Code", token!);
                _emailService.SendEmail(message);

                return StatusCode(StatusCodes.Status201Created, new Response { Message = $"User created and Send OTP to {user.Email} successfully", Status = "Succeeded" });
            }
            if (user != null && await _userManager.CheckPasswordAsync(user, login.Password)) 
            {
                // chaking the password ...

                // clamelist creation
                var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name,user.UserName),
                    new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString())
                };
                //we add roles at the list 
                var userRole = await _userManager.GetRolesAsync(user);
                foreach (var role in userRole)
                {
                    authClaims.Add(new Claim(ClaimTypes.Role, role));
                }
               
                // genarate the token with the claims 
                var jwtToken = GetjwtToken(authClaims);
                // reatring the token
                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(jwtToken),
                    expiration = jwtToken.ValidTo
                }) ;
            }
            return Unauthorized();
            
        }
        //otp login controlers 
        [HttpPost]
        [Route("login_2FA")]
        public async Task<IActionResult> login2fa(string code, string username)
        {
            var user = await _userManager.FindByNameAsync(username);
            var signin = await _signInManager.TwoFactorSignInAsync("Email", code, false, false);
            if (signin.Succeeded)
            {
                if (user != null)
                {
                    // clamelist creation
                    var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name,user.UserName),
                    new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString())
                };
                    //we add roles at the list 
                    var userRole = await _userManager.GetRolesAsync(user);
                    foreach (var role in userRole)
                    {
                        authClaims.Add(new Claim(ClaimTypes.Role, role));
                    }

                    // genarate the token with the claims 
                    var jwtToken = GetjwtToken(authClaims);
                    // reatring the token
                    return Ok(new
                    {
                        token = new JwtSecurityTokenHandler().WriteToken(jwtToken),
                        expiration = jwtToken.ValidTo
                    });
                }
            }
            return StatusCode(StatusCodes.Status201Created, new Response { Message = $"Envalide OTP ", Status = "Error" });
        }


        [HttpPost]
        [Route("ForgetPassword")]
        [AllowAnonymous]
        public async Task<IActionResult> forgaepassword(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passconfirmationLink = Url.Action(nameof(passwordConfirmation), "authentication", new { token, email = user.Email }, Request.Scheme);
                var message = new Message(new string[] { user.Email! }, "Forgore Password  Confirmation Link", passconfirmationLink!);
                _emailService.SendEmail(message);
                return StatusCode(StatusCodes.Status200OK, new Response { Message = $"Forgore Password  Confirmation Link Send to the Email - {user.Email} successfully", Status = "Succeeded" });
            }
            return StatusCode(StatusCodes.Status500InternalServerError, new Response { Message = "Password does not Match", Status = "Error" });
        }


        [HttpGet("Reset_Password")]
        public async Task<IActionResult> passwordConfirmation(string email, string token)
        {
            var model = new Resetpassword { Email = email, Token = token };
            return Ok(new
            {
                model
            });
        }

        [HttpPost]
        [Route("Reset_Password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword(Resetpassword resetPassword)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(resetPassword.Email);
                if (user != null)
                {
                    var changePasswordResult = await _userManager.ResetPasswordAsync(user, resetPassword.Token, resetPassword.Password);
                    if (changePasswordResult.Succeeded)
                    {
                        return StatusCode(StatusCodes.Status200OK, new Response { Message = "Password has been changed successfully!", Status = "Succeeded" });
                    }
                    else
                    {
                        // Password change failed
                        string errorMessage = "Failed to change password. Please try again.";
                        foreach (var error in changePasswordResult.Errors)
                        {
                            errorMessage += $" {error.Description}";
                        }
                        return StatusCode(StatusCodes.Status400BadRequest, new Response { Message = errorMessage, Status = "Error" });
                    }
                }
                else
                {
                    // User not found
                    return StatusCode(StatusCodes.Status404NotFound, new Response { Message = "User not found.", Status = "Error" });
                }
            }
            catch (Exception ex)
            {
                // Return a generic error message
                return StatusCode(StatusCodes.Status500InternalServerError, new Response { Message = "An error occurred while resetting password. Please try again later.", Status = "Error" });
            }
        }

        private JwtSecurityToken GetjwtToken(List<Claim> authClaims)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));
            var token = new JwtSecurityToken(
               issuer: _configuration["JWT:ValidIssuer"],
               audience: _configuration["JWT:ValidAudiance"],
               expires: DateTime.UtcNow.AddHours(1), // Set the expiration time, e.g., expires in 1 hour
               claims: authClaims,
               signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            return token;
        }
    }
}


