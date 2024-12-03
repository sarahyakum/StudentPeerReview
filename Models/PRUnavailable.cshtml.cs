/*
	Written by Darya Anbar for CS 4485.0W1, Senior Design Project, Started November 13, 2024.
    Net ID: dxa200020

    This file defines the model for the Peer Review Success Page.
*/


using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace StudentPR.Pages
{
    public class PRUnavailableModel : PageModel
    {
        // Handles GET requests by returning the Page
        public IActionResult OnGet()
        {
            return Page();
        }
    }
}
