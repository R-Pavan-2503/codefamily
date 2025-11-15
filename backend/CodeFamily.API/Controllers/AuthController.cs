using CodeFamily.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Octokit;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using CodeFamily.Core.Entities;


namespace CodeFamily.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly CodeFamilyDbContext _context;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthController> _logger;
        private readonly GitHubClient _github;

        public AuthController(
            CodeFamilyDbContext context,
            IConfiguration config,
            ILogger<AuthController> logger)
        {
            _context = context;
            _config = config;
            _logger = logger;

            // Initialize a read-only GitHub client
            _github = new GitHubClient(new ProductHeaderValue("CodeFamily"));
        }

        [HttpGet("github-callback")]
        public async Task<IActionResult> GithubCallback([FromQuery] string code)
        {
            try
            {
                // 1. Get our GitHub App credentials from appsettings.json
                // We will add these to appsettings.json in the next task.
                var clientId = _config["GitHub:ClientId"];
                var clientSecret = _config["GitHub:ClientSecret"];

                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
                {
                    _logger.LogError("GitHub ClientId or ClientSecret is not configured.");
                    return StatusCode(500, "Authentication is not configured.");
                }

                // 2. Exchange the temporary 'code' for a permanent 'access_token'
                var tokenRequest = new OauthTokenRequest(clientId, clientSecret, code);
                var token = await _github.Oauth.CreateAccessToken(tokenRequest);

                // 3. Use the access_token to get the user's GitHub profile
                var client = new GitHubClient(new ProductHeaderValue("CodeFamily"));
                client.Credentials = new Credentials(token.AccessToken);
                var githubUser = await client.User.Current();

                // 4. Check if this user already exists in our database
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.GithubId == githubUser.Id);

                if (user == null)
                {
                    // 5a. If they DON'T exist, create a new User
                    user = new Core.Entities.User
                    {
                        Id = Guid.NewGuid(),
                        GithubId = githubUser.Id,
                        Username = githubUser.Login,
                        Email = githubUser.Email,
                        AvatarUrl = githubUser.AvatarUrl,
                        CreatedAt = DateTimeOffset.UtcNow
                    };
                    _context.Users.Add(user);
                }
                else
                {
                    // 5b. If they DO exist, update their info
                    user.Username = githubUser.Login;
                    user.Email = githubUser.Email;
                    user.AvatarUrl = githubUser.AvatarUrl;
                }

                // 6. Save changes to the database
                await _context.SaveChangesAsync();

                // 7. Generate our OWN JWT token for this user
                // We will create this 'GenerateJwtToken' method in a future task.
                var jwtToken = GenerateJwtToken(user);

                // 8. For now, return the GitHub token and user info (we will replace this)
                return Ok(new
                {
                    message = "Login successful!",
                    user = user,
                    token = jwtToken // This will be uncommented later
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during GitHub OAuth callback.");
                return StatusCode(500, "An internal error occurred during authentication.");
            }
        }

        private string GenerateJwtToken(CodeFamily.Core.Entities.User user)
        {
            // 1. Get the secret key from our configuration
            // We will add this to appsettings.json in the next task
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _config["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured.")));

            // 2. Create the signing credentials
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // 3. Create the 'payload' (claims) for the token
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()), // 'Subject' (who the token is for)
                new Claim(JwtRegisteredClaimNames.Name, user.Username),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // 'JWT ID' (a unique ID for this token)
            };

            // 4. Create the token object
            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"], // We will add this to config
                audience: _config["Jwt:Audience"], // We will add this to config
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8), // Token is valid for 8 hours
                signingCredentials: creds
            );

            // 5. Serialize the token into a string
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}