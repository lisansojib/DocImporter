using Microsoft.AspNetCore.Http;

namespace DocImporter.Models
{
    public class UploadBindingModel
    {
        public IFormFile File { get; set; }
    }
}
