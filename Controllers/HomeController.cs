using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using test_task_Mekashron.Models;

namespace test_task_Mekashron.Controllers;

public class HomeController : Controller
{
    [Route("Home/Error")]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}
