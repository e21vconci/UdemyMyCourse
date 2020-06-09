namespace MyCourse.Models.ViewModels
{
    public interface IPaginationInfo
    {
         //Contiene le proprietà indispensabili per il ViewModel
         int CurrentPage { get; }
         int TotalResults { get; }
         int ResultsPerPage { get; }

         string Search { get; }
         string OrderBy { get; }
         bool Ascending { get; }
    }
}