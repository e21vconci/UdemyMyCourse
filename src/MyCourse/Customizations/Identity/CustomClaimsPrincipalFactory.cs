using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

using MyCourse.Models.Entities;

namespace MyCourse.Customizations.Identity
{
    public class CustomClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser>
    {
        public CustomClaimsPrincipalFactory(
            UserManager<ApplicationUser> userManager, IOptions<IdentityOptions> optionsAccessor) 
            : base(userManager, optionsAccessor)
        {
        }
        
        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
        {
            // Aggiungo il mio claim personalizzato
            ClaimsIdentity identity = await base.GenerateClaimsAsync(user);
            identity.AddClaim(new Claim("FullName", user.FullName));
            identity.AddClaim(new Claim(ClaimTypes.Email, user.Email));
            // uso il course service per ottenere gli id dei corsi di cui è autore il docente
            // in questo modo memorizzo nel cookie di autenticazione l'id dei corsi appartenenti al docente
            // identity.AddClaim(new Claim("Authorof", ""));
            return identity;
        }
    }
}