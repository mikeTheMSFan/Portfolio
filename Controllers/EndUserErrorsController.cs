using Microsoft.AspNetCore.Mvc;

namespace Portfolio.Controllers;

public class EndUserErrorsController : Controller
{
    // GET
    [HttpGet]
    public IActionResult NotFoundError()
    {
        return View();
    }

    //GET
    [HttpGet]
    public IActionResult AccessDeniedError()
    {
        return View();
    }
}