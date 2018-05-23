using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QrF.FaceRecForCamera
{
    public partial class Form1 : Form
    {
        private VideoCapture _cameraCapture;

        CascadeClassifier face = new CascadeClassifier("haarcascades/haarcascade_frontalface_default.xml");
        CascadeClassifier eye = new CascadeClassifier("haarcascades/haarcascade_eye.xml");

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            _cameraCapture = new VideoCapture(1);
            Application.Idle += ProcessFrame;
            if (_cameraCapture != null)
                _cameraCapture.Start();//摄像头开启
        }

        private void ProcessFrame(object sender, EventArgs e)
        {
            if (_cameraCapture != null && _cameraCapture.Ptr != IntPtr.Zero)
            {
                var _frame = _cameraCapture.QueryFrame();
                //_cameraCapture.Retrieve(_frame, cbxCameras.SelectedIndex);
                if (_frame != null)
                {
                    using (UMat ugray = new UMat())
                    {
                        CvInvoke.CvtColor(_frame, ugray, ColorConversion.Bgr2Gray);
                        CvInvoke.EqualizeHist(ugray, ugray);
                        Rectangle[] facesDetected = face.DetectMultiScale(
                             ugray,
                             1.1,
                             10,
                             new Size(20, 20));
                        foreach (Rectangle face in facesDetected)
                        {
                            //Get the region of interest on the faces
                            using (UMat faceRegion = new UMat(ugray, face))
                            {
                                Rectangle[] eyesDetected = eye.DetectMultiScale(
                                   faceRegion,
                                   1.1,
                                   10,
                                   new Size(20, 20));

                                foreach (Rectangle eye in eyesDetected)
                                {
                                    Rectangle eyeRect = eye;
                                    eyeRect.Offset(face.X, face.Y);
                                    CvInvoke.Rectangle(_frame, eyeRect, new Bgr(Color.Blue).MCvScalar, 2);
                                }
                            }
                            CvInvoke.Rectangle(_frame, face, new Bgr(Color.Red).MCvScalar, 2);
                        }
                    }
                }
                imageBox1.Image = _frame;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            _cameraCapture.Stop();//摄像头关闭  
            _cameraCapture.Dispose();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = _cameraCapture.QueryFrame().Bitmap;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_cameraCapture != null)
            {
                _cameraCapture.Stop();//摄像头关闭  
                _cameraCapture.Dispose();
            }
        }
    }
}
