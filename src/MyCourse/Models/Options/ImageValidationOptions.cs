using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyCourse.Models.Options
{
    public class ImageValidationOptions
    {
        // Le options vengono definite nel file appsettings.json oppure in sviluppo negli user secrets
        public string Key { get; set; }
        public string Endpoint { get; set; }
        // Soglia oltre la quale il contenuto verrà rifiutato
        public float MaximumAdultScore { get; set; }
    }
}
