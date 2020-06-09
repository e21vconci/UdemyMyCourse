using Microsoft.AspNetCore.Mvc;
using MyCourse.Models.ViewModels;

namespace MyCourse.Customizations.ViewComponents
{
    public class PaginationBarViewComponent : ViewComponent
    {
        //public IViewComponentResult Invoke(CourseListViewModel model)
        //Facciamo dipendere la classe da un'interfaccia(IPaginationInfo) invece che da un'implementazione concreta(CourseListViewModel)
        public IViewComponentResult Invoke(IPaginationInfo model)
        {
            //Il numero di pagina corrente
            //Il numero di risultati totali
            //Il numero di risultati per pagina
            //Search, OrderBy e Ascending
            return View(model);
        }
    }
}