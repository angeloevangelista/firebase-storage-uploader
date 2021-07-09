using Microsoft.AspNetCore.Http;

namespace FirebaseStorageUploader.WebApi.ViewModels
{
  public class FileUploadViewModel
  {
    public IFormFile File { get; set; }
  }
}