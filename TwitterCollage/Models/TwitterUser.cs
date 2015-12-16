using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;

namespace TwitterCollage.Models
{
    public class TwitterUser
    {
        public string ScreenName { get; set; }
        public Image Image { get; set; }
        public string ImageUrl { get; set; }
        public int RetweetsCount { get; set; }

        public void LoadImage()
        {
            if (!String.IsNullOrEmpty(ImageUrl))
            {
                WebClient web = new WebClient();
                byte[] imageBytes = web.DownloadData(ImageUrl);
                MemoryStream ms = new MemoryStream(imageBytes);
                Image = Image.FromStream(ms);
                ms.Close();
            }
            else
            {
                throw new ArgumentException("ScreenName is null or empty");
            }
        }
    }
}