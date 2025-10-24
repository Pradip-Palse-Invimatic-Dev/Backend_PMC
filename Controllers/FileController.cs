using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWebApp.Api.Services;
using MyWebApp.Data;
using MyWebApp.Models;
using MyWebApp.ViewModels;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MyWebApp.Controllers;
[ApiController]
[Route("api/[controller]")]
public class FileController(JWTService jwtService, FileService fileService) : ControllerBase
{
    private readonly string _folderPath = Path.Combine(Directory.GetCurrentDirectory(), "MediaStorage");

    // [HttpGet]
    // [AllowAnonymous]
    // public IActionResult GetFile(string signedToken)
    // {
    //     var jwt = jwtService.ValidateToken(signedToken);
    //     var filePath = jwt.Claims.FirstOrDefault(claim => claim.Type == "filepath")?.Value;
    //     return File(System.IO.File.OpenRead(filePath), "video/mp4", "session.mp4");
    // }

    [HttpPost]
    [AllowAnonymous]
    // [Authorize(Roles = "Doctor, Operator")]   
    [Route("upload")]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        var keyName = await fileService.UploadFile(file);
        return Ok(keyName);
    }

    [HttpGet]
    [AllowAnonymous]
    // [Authorize(Roles = "Doctor, Operator")] 
    [Route("{keyName}/download")]
    public async Task<IActionResult> GetResourceMedia(string keyName)
    {
        var file = Directory.GetFiles(_folderPath, $"{keyName}").FirstOrDefault();
        if (file != null)
        {
            var fileBytes = System.IO.File.ReadAllBytes(file);
            var contentType = GetContentType(file);
            var fileName = Path.GetFileName(file);

            // Set Content-Disposition header to inline
            Response.Headers.Add("Content-Disposition", "inline; filename=" + fileName);

            return File(fileBytes, contentType);
        }
        else
        {
            return NotFound(); // File not found
        }                               
    }

    private string GetContentType(string fileName)
    {
        string fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
        switch (fileExtension)
        {
            case ".pdf":
                return "application/pdf";
            case ".jpg":
            case ".jpeg":
                return "image/jpeg";
            case ".png":
                return "image/png";
            default:
                return "application/octet-stream"; // Default content type
        }
    }

    [AllowAnonymous]
    [HttpGet]
    [Route("self-declaration-cert")]
    public async Task<IActionResult> GetSelfDeclarationCert()
    {
        var filePath = Path.Combine(_folderPath, "AdminDocs", "self_declaration_cert.pdf");
        if (System.IO.File.Exists(filePath))
        {
            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            var contentType = "application/pdf";
            var fileName = "self_declaration_cert.pdf";

            // Set Content-Disposition header to inline
            Response.Headers.Add("Content-Disposition", "inline; filename=" + fileName);

            return File(fileBytes, contentType);
        }
        else
        {
            return NotFound(); // File not found
        }
    }

}
