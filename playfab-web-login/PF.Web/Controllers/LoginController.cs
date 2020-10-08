using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PF.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> SignIn([FromForm] string email, [FromForm] string password)
        {
            // This should be be initialized on Startup
            PlayFabSettings.staticSettings.TitleId = "Insert your Title ID";

            var response = await PlayFabClientAPI.LoginWithEmailAddressAsync(new LoginWithEmailAddressRequest { Email = email, Password = password });

            // If it fails to login, it creates the account, this is NOT the recommended way to handle this.
            // It was done only to speedup the account creation for this spike
            if (response.Result == null)
            {
                var creaResponse = await PlayFabClientAPI.RegisterPlayFabUserAsync(new RegisterPlayFabUserRequest { Email = email, Password = password, RequireBothUsernameAndEmail = false });

                if (creaResponse.Result == null)
                    return BadRequest();

                response = await PlayFabClientAPI.LoginWithEmailAddressAsync(new LoginWithEmailAddressRequest { Email = email, Password = password });

                if (response.Result == null)
                    return BadRequest();
            }

            var claimsIdentity = new ClaimsIdentity(
                // Add any claim related to the user
                new List<Claim>
                {
                    new Claim(ClaimTypes.Name, email),
                    new Claim(ClaimTypes.Role, "User")
                }, 
                CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                // Persist the authentication (usually we get this value from the 'Remember me' checkbox)
                IsPersistent = true,
            };

            authProperties.StoreTokens(new List<AuthenticationToken> { 
                new AuthenticationToken
                {
                    Name = "PlayFab",
                    Value = response.Result.EntityToken.EntityToken
                }
            });

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);


            return Ok();
        }

        [HttpDelete]
        public async Task<IActionResult> SignOut()
        {
            await HttpContext.SignOutAsync();
            return Ok();
        }
    }
}
