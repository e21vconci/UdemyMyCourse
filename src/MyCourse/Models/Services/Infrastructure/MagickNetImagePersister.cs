using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using ImageMagick;
using System.Threading;
using System;
using MyCourse.Models.Exceptions.Infrastructure;

namespace MyCourse.Models.Services.Infrastructure
{
    public class MagickNetImagePersister : IImagePersister
    {
        private readonly SemaphoreSlim semaphore;
        private readonly IWebHostEnvironment env;

        public MagickNetImagePersister(IWebHostEnvironment env)
        {
            //contro immagini di dimen
            ResourceLimits.Width = 4000;
            ResourceLimits.Height = 4000;
            semaphore = new SemaphoreSlim(2);
            this.env = env;
        }

        public async Task<string> SaveCourseImageAsync(int courseId, IFormFile formFile)
        {
            //Il metodo WaitAsync ha anche un overload che permette di passare un timeout
            //Ad esempio, se vogliamo aspettare al massimo 1 secondo:
            //await semaphore.AwaitAsync(TimeSpan.FromSeconds(1));
            //Se il timeout scade, il SemaphoreSlim solleverà un'eccezione (così almeno non resta in attesa all'infinito)
            await semaphore.WaitAsync();
            try 
            {
                //Salavare il file
                string path = $"/Courses/{courseId}.jpg";
                string physicalPath = Path.Combine(env.WebRootPath, "Courses", $"{courseId}.jpg");
            
                using Stream inputStream = formFile.OpenReadStream();
                using MagickImage image = new MagickImage(inputStream);

                //Manipolare l'immagine
                int width = 300;  //Ottenere questi valori dalla configurazione e non cablarli nel codice.
                int height = 300;
                MagickGeometry resizeGeometry = new MagickGeometry(width, height)
                {
                    FillArea = true
                };

                image.Resize(resizeGeometry);
                image.Crop(width, width, Gravity.Northwest);

                image.Quality = 70;
                image.Write(physicalPath, MagickFormat.Jpg);

                //Restituire il percorso al file
                return path;
            }
            catch (Exception exc)
            {
                throw new ImagePersistenceException(exc);
            }
            finally 
            {
                semaphore.Release();
            }
        }
    }
}