using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Data;
using Portfolio.Models;
using Portfolio.Services.Interfaces;
using Portfolio.ViewModels;

namespace Portfolio.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IBlogEmailSender _emailSender;

    public HomeController(IBlogEmailSender emailSender,
        ApplicationDbContext context)
    {
        _emailSender = emailSender;
        _context = context;
    }

    [TempData(Key = "StatusMessage")] public string StatusMessage { get; set; } = "";

    public IActionResult Index()
    {
        //get all projects
        var projects = _context.Projects.OrderByDescending(p => p.Created).Take(5).ToList();

        //store data to be returned to the view.
        ViewData["Projects"] = projects;

        //return to default view.
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Contact()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact([FromForm] ContactViewModel model)
    {
        try
        {
            //set body of contact email.
            model.Body =
                $"<p>New Message From User ✅✅✅</p><p>Email:{model.Email}</p><p>Phone Number: {model.PhoneNumber}</p>Body:</p><p>{model.Body}</p>";

            //send contact email.
            await _emailSender.SendContactEmailAsync(model.Email, model.Name, model.Subject, model.Body);
        }
        catch (Exception)
        {
            //let the user know there was an error.
            StatusMessage =
                "Error: There was an error sending the message, if this persists please let the administrator know.";
            return RedirectToAction("Index");
        }

        //let the user know the message was sent.
        StatusMessage = "Your message has been received, I will get back to you soon.";
        return RedirectToAction("Index");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}