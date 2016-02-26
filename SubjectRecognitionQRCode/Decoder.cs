using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Diagnostics;


using ZXing;
using ZXing.Common;
using ZXing.QrCode;

namespace SubjectRecognitionQRCode
{
    class Decoder
    {
          private Bitmap srcPicture;

        public void setPicture(Bitmap pic)
        {
            this.srcPicture = pic;
        }

        public Bitmap getPicture()
        {
            return this.srcPicture;
        }

        public Dictionary<string, ResultPoint[]> MDecode(Bitmap pic)
        {
            this.srcPicture = pic;

            Dictionary<string, ResultPoint[]> decode = new Dictionary<string,ResultPoint[]>();

            try
            {
                decode = findQrCodeText(new QRCodeReader(), this.srcPicture);
            }
            catch (Exception exp)
            {
                decode = null;
            }

            return decode;
        }

        public Dictionary<string, ResultPoint[]> findQrCodeText(Reader decoder, Bitmap bitmap)
        {
            var rgb = new ZXing.RGBLuminanceSource(GetRGBValues(bitmap), bitmap.Width, bitmap.Height);
            var hybrid = new HybridBinarizer(rgb);
            BinaryBitmap binBitmap = new BinaryBitmap(hybrid);

            Dictionary<string, ResultPoint[]> decodedString = new Dictionary<string, ResultPoint[]>();

            Result res=decoder.decode(binBitmap, null);

            if (res != null)
            {
                decodedString.Add(res.Text,res.ResultPoints);
            }
            return decodedString;
        }

        


        private byte[] GetRGBValues(Bitmap bmp)
        {
            // Lock the bitmap's bits. 
            System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height);
            System.Drawing.Imaging.BitmapData bmpData = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly, bmp.PixelFormat);

            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;

            // Declare an array to hold the bytes of the bitmap.
            int bytes = bmpData.Stride * bmp.Height;
            byte[] rgbValues = new byte[bytes];
            // Copy the RGB values into the array.
            System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);
            bmp.UnlockBits(bmpData);

            return rgbValues;
        }
    
    }
}
