using Microsoft.AspNetCore.Mvc;

namespace FitBarbs.Web.ViewComponents;

public class ToggleViewComponent : ViewComponent
{
    public record ToggleParams(string Name, string Id, bool IsChecked, string? Label, string? Hint, string? DescribedBy);

    public IViewComponentResult Invoke(string name, string id, bool isChecked, string? label = null, string? hint = null, string? describedBy = null)
    {
        var model = new ToggleParams(name, id, isChecked, label, hint, describedBy);
        return View(model);
    }
}


