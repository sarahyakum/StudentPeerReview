/*
    Written by Kiara Vaz for CS 4485.0W1, Senior Design Project, Started October 20, 2024.
    Net ID: kmv200000

    This file defines the model for viewing the application's Privacy Policy.
*/


using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace StudentPR.Pages;

public class PrivacyModel : PageModel
{
    private readonly ILogger<PrivacyModel> _logger;

    public PrivacyModel(ILogger<PrivacyModel> logger)
    {
        _logger = logger;
    }

    // Handles GET requests by returning the Page
    public IActionResult OnGet()
    {
        return Page();
    }
}

