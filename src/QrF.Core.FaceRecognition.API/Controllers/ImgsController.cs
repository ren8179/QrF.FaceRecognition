using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Transforms;

namespace QrF.Core.FaceRecognition.API.Controllers
{
    [Consumes("application/json", "multipart/form-data")]
    [Route("api/[controller]")]
    public class ImgsController : Controller
    {
        private const string UPLOAD_FOLDER = "upload";
        private const string RESIZE_FOLDER = "resize";
        private readonly IHostingEnvironment _hostingEnvironment;

        public ImgsController(IHostingEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
        }

        [HttpPost]
        [Route("resizeimg")]
        public async Task<IActionResult> PostAsync(List<IFormFile> files)
        {
            try
            {
                string webRootPath = _hostingEnvironment.WebRootPath;
                var uploadPath = Path.Combine(webRootPath, UPLOAD_FOLDER);
                var resizePath = Path.Combine(webRootPath, RESIZE_FOLDER);
                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);
                if (!Directory.Exists(resizePath))
                    Directory.CreateDirectory(resizePath);
                var list = new List<string>();
                foreach (var formFile in files)
                {
                    if (formFile.Length < 1)
                        continue;
                    string fileExt = Path.GetExtension(formFile.FileName);
                    if (!(new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".ico" }).Contains(fileExt?.ToLower()))
                        continue;
                    if (formFile.Length > 1024 * 1024 * 100)
                        throw new Exception("文件不得超过100M");
                    var newFileName = Guid.NewGuid().ToString() + fileExt;
                    var filePath = Path.Combine(uploadPath, newFileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await formFile.CopyToAsync(stream);
                        ImgHandler(formFile.OpenReadStream(), fileExt, Path.Combine(resizePath, newFileName));
                    }
                    list.Add($"http://{Request.Host}/src/{RESIZE_FOLDER}/{newFileName}");
                }
                return Ok(new { count = list.Count, list });
            }
            catch (Exception ex)
            {
                return BadRequest(new { msg = ex.Message });
            }
        }
        /// <summary>
        /// 改变图片分辨率
        /// </summary>
        /// <param name="input"></param>
        /// <param name="fileExt"></param>
        /// <param name="savePath"></param>
        private void ImgHandler(Stream input, string fileExt, string savePath)
        {
            var min = new { w = 640, h = 480 };
            var max = new { w = 2016, h = 3840 };
            var sta = new { w = 1024, h = 768 };
            var image = Image.Load(input, GetDecoder(fileExt));
            int w = image.Width, h = image.Height;
            if (w < min.w || w > max.w)
            {
                h = sta.w * h / w;
                w = sta.w;
            }
            if (h < min.h || h > max.h)
            {
                w = sta.h * w / h;
                h = sta.h;
            }
            image.Mutate(x => x.Resize(w, h));
            image.Save(savePath);
        }

        private IImageDecoder GetDecoder(string fileExt)
        {
            switch (fileExt)
            {
                case ".png":
                    return new SixLabors.ImageSharp.Formats.Png.PngDecoder();
                case ".bmp":
                    return new SixLabors.ImageSharp.Formats.Bmp.BmpDecoder();
                case ".gif":
                    return new SixLabors.ImageSharp.Formats.Gif.GifDecoder();
                default:
                    return new SixLabors.ImageSharp.Formats.Jpeg.JpegDecoder();
            }
        }
    }
}
