using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize(Roles = "User")]
public class UserController : Controller
{
    public IActionResult Panel()
    {
        return View();
    }
}
