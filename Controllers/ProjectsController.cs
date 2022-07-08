using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Data;
using Portfolio.Enums;
using Portfolio.Extensions;
using Portfolio.Models.Content;
using Portfolio.Services.Interfaces;

namespace Portfolio.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
public class ProjectsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IRemoteImageService _remoteImageService;
    private readonly IValidate _validate;

    public ProjectsController(ApplicationDbContext context, IRemoteImageService remoteImageService, IValidate validate)
    {
        _context = context;
        _remoteImageService = remoteImageService;
        _validate = validate;
    }

    // GET: Projects/Create
    [Authorize(Roles = "Administrator")]
    public IActionResult Create()
    {
        return View();
    }

    // POST: Projects/Create
    // To protect from over-posting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Create([Bind("Id,Type,Title,Image,Description,ProjectUrl")] Project project)
    {
        if (ModelState.IsValid)
        {
            //uses the validate service to make sure the project passes all checks.
            var result = _validate.ProjectEntry(project);

            //determines if there are model errors, if so, they will be added
            //to model state errors.
            var thereAreModelErrors = CheckForModelErrors(result);

            //if there are model errors, return the errors with the blog to the default view.
            if (thereAreModelErrors) return View(project);

            //update image if necessary.
            if (project.Image != null)
                project.ContentUrl = _remoteImageService.UploadContentImage(project.Image, ContentType.Project);
            project.Created = DateTime.Now.ToUniversalTime();

            //Add entity to the queue for creation to the db.
            _context.Add(project);

            //apply all queued actions to the db.
            await _context.SaveChangesAsync();

            //return user to the index view.
            return RedirectToAction(nameof(Index));
        }

        return View(project);
    }

    // GET: Projects/Delete/5
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Delete(Guid? id)
    {
        if (id == null) return NotFound();

        var project = await _context.Projects
            .FirstOrDefaultAsync(m => m.Id == id);

        if (project == null) return NotFound();
        return View(project);
    }

    // POST: Projects/Delete/5
    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        //if project is not found, return 404 error.
        if (ProjectExists(id) == false) return NotFound();

        //get project by id.
        var project = await _context.Projects.FindAsync(id);

        //delete project image.
        DeleteProjectImage(project!);

        //add entity to be removed from the db.
        _context.Projects.Remove(project!);

        //process all actions to the db.
        await _context.SaveChangesAsync();

        //return user to the index view.
        return RedirectToAction(nameof(Index));
    }

    // GET: Projects/Edit/5
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Edit(Guid id)
    {
        //if project is not found, return 404 error.
        if (ProjectExists(id) == false) return NotFound();

        //get project by id.
        var project = await _context.Projects.FindAsync(id);

        //return default view with project.
        return View(project);
    }

    // POST: Projects/Edit/5
    // To protect from over-posting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Edit(Guid id,
        [Bind("Id,Type,Title,Image,Description,ProjectUrl")]
        Project project)
    {
        if (ProjectExists(id) == false) return NotFound();
        //get project to be edited by project id.
        var newProject = await _context.Projects.FirstOrDefaultAsync(p => p.Id == id);
        if (ModelState.IsValid)
        {
            //set new project properties.
            newProject = SetNewProject(project, newProject!);

            //uses the validate service to make sure the project passes all checks.
            var result = _validate.ProjectEntry(newProject);

            //determines if there are model errors, if so, they will be added
            //to model state errors.
            var thereAreModelErrors = CheckForModelErrors(result);

            //if there are model errors, return the errors with the blog to the default view.
            if (thereAreModelErrors) return View(project);

            //update image if necessary.
            if (newProject.Image == null)
            {
                //do nothing...
            }
            
            else if (newProject.Image != null && string.IsNullOrEmpty(newProject.ContentUrl) == false)
            {
                newProject = (_remoteImageService.UpdateImage(newProject, ContentType.Project, newProject.Image) as Project)!;
            }

            else
            {
                newProject.ContentUrl = _remoteImageService.UploadContentImage(newProject.Image!, ContentType.Project);
            }

            //add entity to be updated in the db.
            _context.Update(newProject);

            //process all actions to the db.
            await _context.SaveChangesAsync();

            //redirect user to index.
            return RedirectToAction(nameof(Index));
        }

        //Model state is not valid, return user to edit view.
        return View(newProject);
    }

    [HttpGet]
    public IActionResult GetTop5ProjectsByDate()
    {
        //get top 5 projects from date
        var output = _context.Projects
            .OrderByDescending(p => p.Created).Take(5).ToList();

        //return top 5 project in json form.
        return Json(new { projects = output });
    }

    // GET: Projects
    public async Task<IActionResult> Index()
    {
        //get all projects
        var projects = _context.Projects;

        //return project as a list to default view.
        return View(await projects.ToListAsync());
    }

    private bool CheckForModelErrors(Tuple<List<Tuple<string, string, Project>>> result)
    {
        //if there are errors, let the user know.
        if (result.Item1.Any())
        {
            var modelErrors = result.Item1.ToList();
            foreach (var modelError in modelErrors) ModelState.AddModelError(modelError.Item1, modelError.Item2);

            return true;
        }

        return false;
    }

    private void DeleteProjectImage(Project project)
    {
        if (!string.IsNullOrEmpty(project.ContentUrl))
        {
            var configuration = GetConfiguration();
            //Set upload directory
            var uploadDirectory = configuration.GetSection("SftpSettings").GetChildren()
                .FirstOrDefault(d => d.Key == "ProjectUploadDirectory")!.Value;

            //delete remote picture
            _remoteImageService.CheckForImageToDelete(project, uploadDirectory);
        }
    }

    private bool ProjectExists(Guid id)
    {
        if (_context.Projects.Any(e => e.Id == id))
            return true;
        return false;
    }

    private Project SetNewProject(Project project, Project newProject)
    {
        newProject.Id = project.Id;
        newProject.Type = project.Type;
        newProject.Title = project.Title;
        newProject.Image = project.Image;
        newProject.Description = project.Description;
        newProject.ProjectUrl = project.ProjectUrl;

        return newProject;
    }

    private IConfigurationRoot GetConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build()
            .Decrypt("CipherKey", "CipherText:");

        return configuration;
    }
}