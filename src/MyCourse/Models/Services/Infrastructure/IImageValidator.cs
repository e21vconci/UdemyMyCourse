using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace MyCourse.Models.Services.Infrastructure
{
    public interface IImageValidator
    {
        Task<bool> IsValidAsync(IFormFile formFile);
    }
}
