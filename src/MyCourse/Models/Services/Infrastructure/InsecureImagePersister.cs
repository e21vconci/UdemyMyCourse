using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace MyCourse.Models.Services.Infrastructure
{
    public class InsecureImagePersister : IImagePersister
    {
        private readonly IWebHostEnvironment env;

        public InsecureImagePersister(IWebHostEnvironment env)
        {
            this.env = env;
        }

        public async Task<string> SaveCourseImageAsync(int courseId, IFormFile formFile)
        {
            //TODO: Salavare il file
            string path = $"/Courses/{courseId}.jpg";
            string physicalPath = Path.Combine(env.WebRootPath, "Courses", $"{courseId}.jpg");
            //Utilizziamo using in modo tale che FileStream invochi il metodo dispose
            using (FileStream fileStream = File.OpenWrite(physicalPath))
            {
                await formFile.CopyToAsync(fileStream);
            }

            //TODO: Restituire il percorso al file
            return path;
        }
    }
}