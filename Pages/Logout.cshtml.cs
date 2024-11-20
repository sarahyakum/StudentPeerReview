using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace StudentPR.Pages
{
    public class LogoutModel : PageModel
    {
        public IActionResult OnPost() // user wants to logout
        {
            HttpContext.Session.Clear();
            return RedirectToPage("/Login");
        }
    }
}
