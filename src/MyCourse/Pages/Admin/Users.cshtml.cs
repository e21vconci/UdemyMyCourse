using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyCourse.Models.Entities;
using MyCourse.Models.Enums;
using MyCourse.Models.InputModels.Users;

namespace MyCourse.Pages.Admin
{
    [Authorize(Roles = nameof(Role.Administrator))]
    public class UsersModel : PageModel
    {
        public readonly UserManager<ApplicationUser> userManager;
        public UsersModel(UserManager<ApplicationUser> userManager)
        {
            this.userManager = userManager;
        }

        [BindProperty]
        public UserRoleInputModel Input { get; set; }
        public IList<ApplicationUser> Users { get; private set; }

        // Il tipo di questa proprietà è Role in modo da evitare la sanitizzazione perchè in questo modo viene già eseguita
        // BindProperty per default funziona solo con richieste post. Se vogliamo che funzioni con richieste get,
        // dobbiamo aggiungere la seguente proprietà.
        [BindProperty(SupportsGet = true)]
        public Role InRole { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            ViewData["Title"] = "Gestione Utenti";
            // Con la proprietà InRole ricaviamo il ruolo in modo dinamico
            Claim claim = new Claim(ClaimTypes.Role, InRole.ToString());
            Users = await userManager.GetUsersForClaimAsync(claim);
            return Page();
        }

        // Page handler per i bottoni 
        // asp-page-handler="Assign"
        public async Task<IActionResult> OnPostAssignAsync()
        {
            // Verifichiamo se ModelState.IsValid è true (indirizzo email e ruolo utente)
            if (!ModelState.IsValid)
            {
                return await OnGetAsync();
            }

            // Con lo UserManager recuperiamo l'utente dal database in base all'email
            ApplicationUser user = await userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                ModelState.AddModelError(nameof(Input.Email), $"L'indirizzo email {Input.Email} non corrisponde ad alcun utente");
                return await OnGetAsync();
            }

            // Con lo UserManager recuperiamo gli attuali claim dell'utente
            IList<Claim> claims = await userManager.GetClaimsAsync(user);
            // Verifiachiamo se quell'utente ha già quel ruolo
            Claim roleClaim = new Claim(ClaimTypes.Role, Input.Role.ToString());
            // Verifico se la lista dei claim dell'utente contiene il claim Role e se il valore del claim Role 
            // è già stato assegnato all'utente
            if (claims.Any(claim => claim.Type == roleClaim.Type && claim.Value == roleClaim.Value))
            {
                // Se ce l'ha diamo un errore
                ModelState.AddModelError(nameof(Input.Role), $"Il ruolo {Input.Role} è già assegnato all'utente {Input.Email}");
                return await OnGetAsync();
            }

            // Se non ce l'ha aggiungiamo il claim del ruolo
            IdentityResult result = await userManager.AddClaimAsync(user, roleClaim);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, $"L'operazione è fallita: {result.Errors.FirstOrDefault()?.Description}");
                return await OnGetAsync();
            }

            // Diamo conferma all'utente e lo reindirizziamo 
            // Messaggio TempData contenuto nella view _Layout.cshtml
            TempData["ConfirmationMessage"] = $"Il ruolo {Input.Role} è stato assegnato all'utente {Input.Email}";
            // Il redirect al posto di OnGet azzera il form in modo che l'utente potrà inserire nuovamente altri valori
            // Forniamo al redirect un route value per InRole in modo da non perdere la chiave query string
            return RedirectToPage(new { inrole = (int) InRole });
        }

        // asp-page-handler="Revoke"
        public async Task<IActionResult> OnPostRevokeAsync()
        {
            // Verifichiamo se ModelState.IsValid è true (indirizzo email e ruolo utente)
            if (!ModelState.IsValid)
            {
                return await OnGetAsync();
            }

            // Con lo UserManager recuperiamo l'utente dal database in base all'email
            ApplicationUser user = await userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                ModelState.AddModelError(nameof(Input.Email), $"L'indirizzo email {Input.Email} non corrisponde ad alcun utente");
                return await OnGetAsync();
            }

            // Con lo UserManager recuperiamo gli attuali claim dell'utente
            IList<Claim> claims = await userManager.GetClaimsAsync(user);
            // Verifiachiamo se l'utente non ha quel ruolo
            Claim roleClaim = new Claim(ClaimTypes.Role, Input.Role.ToString());
            // Verifico se la lista dei claim dell'utente non contiene il claim Role e se il valore del claim Role 
            // non è stato assegnato all'utente
            if (!claims.Any(claim => claim.Type == roleClaim.Type && claim.Value == roleClaim.Value))
            {
                // Se non ce l'ha diamo un errore
                ModelState.AddModelError(nameof(Input.Role), $"Il ruolo {Input.Role} non era assegnato all'utente {Input.Email}");
                return await OnGetAsync();
            }

            // Se ce l'ha rimuoviamo il claim del ruolo
            IdentityResult result = await userManager.RemoveClaimAsync(user, roleClaim);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, $"L'operazione è fallita: {result.Errors.FirstOrDefault()?.Description}");
                return await OnGetAsync();
            }

            // Diamo conferma all'utente e lo reindirizziamo 
            // Messaggio TempData contenuto nella view _Layout.cshtml
            TempData["ConfirmationMessage"] = $"Il ruolo {Input.Role} è stato revocato all'utente {Input.Email}";
            // Il redirect al posto di OnGet azzera il form in modo che l'utente potrà inserire nuovamente altri valori
            // Forniamo al redirect un route value per InRole in modo da non perdere la chiave query string
            return RedirectToPage(new { inrole = (int) InRole });
        }
    }
}
