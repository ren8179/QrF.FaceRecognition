using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace QrF.FaceRecognition.API.Controllers
{
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
            Bitmap img1 = new Bitmap(input);
            Bitmap img2 = new Bitmap(sta.w, sta.h, PixelFormat.Format24bppRgb);
            img2.SetResolution(sta.w, sta.h);
            using (Graphics g = Graphics.FromImage(img2))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Default;
                g.DrawImage(img1, new Rectangle(0, 0, img2.Width, img2.Height), 0, 0, img1.Width, img1.Height, GraphicsUnit.Pixel);
                g.Dispose();

                img2.Save(savePath, GetFormat(fileExt));
            }
        }
        /// <summary>  
        /// 重绘图片 
        /// </summary>  
        /// <param name="sourceFile">原始图片文件</param>  
        /// <param name="quality">质量压缩比 1-100</param>  
        /// <param name="multiple">收缩倍数</param> 
        /// <returns>成功返回true,失败则返回false</returns>  
        public void GraphicsImage(String sourceFile, long quality, int multiple = 1)
        {
            long imageQuality = quality;
            multiple = multiple < 1 ? 1 : multiple;
            Bitmap newImage = null;
            using (var sourceImage = new Bitmap(sourceFile))
            {
                var xWidth = sourceImage.Width / multiple;
                var yWidth = sourceImage.Height / multiple;
                newImage = new Bitmap(xWidth, yWidth);
                var g = Graphics.FromImage(newImage);
                g.DrawImage(sourceImage, 0, 0, xWidth, yWidth);
                g.Dispose();
            }
            var myImageCodecInfo = GetEncoderInfo("image/jpeg");
            var myEncoderParameters = new EncoderParameters(1);
            myEncoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, imageQuality);
            newImage.Save(sourceFile, myImageCodecInfo, myEncoderParameters);
        }
        /// <summary>  
        /// 获取图片编码信息  
        /// </summary>  
        private ImageCodecInfo GetEncoderInfo(String mimeType)
        {
            int j;
            ImageCodecInfo[] encoders;
            encoders = ImageCodecInfo.GetImageEncoders();
            for (j = 0; j < encoders.Length; ++j)
            {
                if (encoders[j].MimeType == mimeType)
                    return encoders[j];
            }
            return null;
        }
        /// <summary>
        /// 保存图片
        /// </summary>
        public ImageFormat GetFormat(string strExt)
        {
            ImageFormat imgFormat = ImageFormat.Jpeg;
            switch (strExt)
            {
                case ".bmp":
                    imgFormat = ImageFormat.Bmp;
                    break;
                case ".gif":
                    imgFormat = ImageFormat.Gif;
                    break;
                case ".png":
                    imgFormat = ImageFormat.Png;
                    break;
            }
            return imgFormat;
        }

    }
}
