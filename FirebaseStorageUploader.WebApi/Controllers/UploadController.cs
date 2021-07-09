using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Firebase.Auth;
using Firebase.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using FirebaseStorageUploader.WebApi.ViewModels;

namespace FirebaseStorageUploader.WebApi.Controllers
{
  [ApiController]
  public class UploadController : ControllerBase
  {
    private readonly IConfiguration _configuration;

    public UploadController(IConfiguration configuration)
    {
      this._configuration = configuration;
    }

    [HttpPost]
    [Route("api/v1/uploads")]
    public ActionResult UploadFile(
      [FromForm] FileUploadViewModel fileUploadViewModel
    )
    {
      var tempPath = this._configuration
        .GetSection("Directories")
        .GetValue<string>("TempPath");

      var firebaseFolderName = this._configuration
        .GetSection("Directories")
        .GetValue<string>("FirebaseFolderName");

      var file = fileUploadViewModel.File;

      if (file == null || file.Length <= 0)
        return BadRequest(new { Message = "Não mandou o arquivo, imbecil." });

      var firebasePath = Path.Combine(tempPath, firebaseFolderName);

      if (!Directory.Exists(firebasePath))
        Directory.CreateDirectory(firebasePath);

      var currentTimeStamps = ((DateTimeOffset)DateTime.UtcNow)
        .ToUnixTimeSeconds();

      string uploadedFileLink;
      string fileName = $"{currentTimeStamps}-{file.FileName}";
      string uploadedFilePath = Path.Combine(firebasePath, fileName);

      using (var fileStream = new FileStream(uploadedFilePath, FileMode.Create))
        file.CopyTo(fileStream);

      var apiKey = this._configuration
        .GetSection("Firebase")
        .GetValue<string>("ApiKey");

      var authEmail = this._configuration
        .GetSection("Firebase")
        .GetValue<string>("AuthEmail");

      var authPassword = this._configuration
        .GetSection("Firebase")
        .GetValue<string>("AuthPassword");

      var authProvider = new FirebaseAuthProvider(
        new FirebaseConfig(apiKey)
      );

      var firebaseAuthLink = authProvider
        .SignInWithEmailAndPasswordAsync(authEmail, authPassword)
        .GetAwaiter()
        .GetResult();

      var cancellationTokenSource = new CancellationTokenSource();

      var bucket = this._configuration
        .GetSection("Firebase")
        .GetValue<string>("Bucket");

      using (
        var openFileStream = new FileStream(uploadedFilePath, FileMode.Open)
      )
      {
        var upload = new FirebaseStorage(
          bucket,
          new FirebaseStorageOptions()
          {
            AuthTokenAsyncFactory = () => Task.FromResult(
              firebaseAuthLink.FirebaseToken
            ),
            ThrowOnCancel = true
          }
        )
        .Child("assets")
        .Child(fileName)
        .PutAsync(openFileStream, cancellationTokenSource.Token);

        uploadedFileLink = upload.GetAwaiter().GetResult();

        return Ok(new { Ok = uploadedFileLink });
      }
    }
  }
}
