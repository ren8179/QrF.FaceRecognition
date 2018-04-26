using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace QrF.FaceRecognition.API.Controllers
{
    [Route("api/[controller]")]
    public class FacesController : Controller
    {
        private const string UPLOAD_FOLDER = "upload";
        private const string RESIZE_FOLDER = "resize";
        private readonly IHostingEnvironment _hostingEnvironment;

        public FacesController(IHostingEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
        }

        [HttpPost]
        [Route("detection")]
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
                var msg = "";
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
                        await Task.Delay(3000).ContinueWith(result =>
                        {
                            msg += newFileName + ImgHandler(filePath, resizePath, fileExt, list);
                        });
                    }

                }
                return Ok(new { count = list.Count, msg = msg, list });
            }
            catch (Exception ex)
            {
                return BadRequest(new { msg = ex.Message });
            }
        }
        /// <summary>
        /// 人脸识别
        /// </summary>
        private string ImgHandler(string filePath, string resizePath, string fileExt, List<string> list)
        {
            Mat image = new Mat(filePath);
            var img = new Image<Bgr, byte>(image.Bitmap);
            var faces = Detect(image, "Data/haarcascade_frontalface_default.xml", out long detectionTime);
            foreach (Rectangle face in faces)
            {
                Image<Bgr, byte> Sub = img.GetSubRect(face);
                Image<Bgr, byte> CropImage = new Image<Bgr, byte>(Sub.Size);
                CvInvoke.cvCopy(Sub, CropImage, IntPtr.Zero);
                var newFileName = Path.Combine(resizePath, Guid.NewGuid().ToString() + fileExt);
                CropImage.Save(newFileName);
                list.Add($"http://{Request.Host}/src/{RESIZE_FOLDER}/{newFileName}");
            }
            return $"识别耗时：{detectionTime / 1000} 秒; ";
        }
        private List<Rectangle> Detect(Mat image, String faceFileName, out long detectionTime)
        {
            Stopwatch watch;
            var faces = new List<Rectangle>();
            using (CascadeClassifier face = new CascadeClassifier(faceFileName))
            {
                watch = Stopwatch.StartNew();
                using (UMat ugray = new UMat())
                {
                    CvInvoke.CvtColor(image, ugray, Emgu.CV.CvEnum.ColorConversion.Bgr2Gray);

                    //normalizes brightness and increases contrast of the image
                    CvInvoke.EqualizeHist(ugray, ugray);

                    //Detect the faces  from the gray scale image and store the locations as rectangle
                    //The first dimensional is the channel
                    //The second dimension is the index of the rectangle in the specific channel
                    Rectangle[] facesDetected = face.DetectMultiScale(
                       ugray,
                       1.2,
                       5,
                       new Size(32, 32));

                    faces.AddRange(facesDetected);
                }
                watch.Stop();
            }
            detectionTime = watch.ElapsedMilliseconds;
            return faces;
        }
    }
}
