using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace StudentPR.Pages
{
    public class ScoresModel : PageModel
    {
        public IActionResult OnGet()
        {
            return Page();
        }
    }
}
