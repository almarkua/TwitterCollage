using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;
using LinqToTwitter;
using TwitterCollage.Models;

namespace TwitterCollage.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult RetweetedUsers(string ScreenName)
        {
            var users = GetRetweetedUsers(ScreenName).Values.ToList();
            var url = CreateCollage(users);
            return View(url as object);
        }

        private string CreateCollage(List<TwitterUser> users)
        {
            var collageSize = 300;
            Bitmap img = new Bitmap(collageSize,collageSize);
            Graphics gr = Graphics.FromImage(img);
            gr.FillRectangle(new SolidBrush(Color.White), new Rectangle(0,0,img.Width,img.Height));
            int countInLine = (int)Math.Ceiling(Math.Sqrt(users.Count));
            int size = (collageSize - countInLine + 1)/countInLine;
            int[] freeSpaces = new int[countInLine];

            for (int i = 0; i < countInLine; i++)
            {
                for (int column = countInLine; column > 0; column--)
                {
                    if (column*countInLine + i+1 <= users.Count)
                    {
                        freeSpaces[i] = ((countInLine - column) - 1)*size/countInLine;
                        break;
                    }
                }
            }
            for (int i=0;i< countInLine; i++) {
                for (int j = 0; j < countInLine; j++)
                {
                    if (i*countInLine + j >= users.Count) break;
                    users[i * countInLine + j].LoadImage();
                    users[i*countInLine + j].Image = CropAsSquare(users[i*countInLine + j].Image);
                    //var x = (j > 0) ? i*size + i + j*freeSpaces[j - 1] : i*size + i;
                    var x = i*size + i + (i + 1)*freeSpaces[j];
                    gr.DrawImage(users[i*countInLine+j].Image,new Rectangle(x,j*size+j*1,size,size));
                }
            }
            gr.Flush();
            var env = HostingEnvironment.ApplicationPhysicalPath + "Content\\collages\\collage.jpg";
            img.Save(env, ImageFormat.Jpeg);
            gr.Dispose();
            img.Dispose();
            return "../Content/collages/collage.jpg";
        }

        public Bitmap ResizeImage(Image image, int width, int height)
        {
            if (width == 0) width = 2;
            if (height == 0) height = 2;
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        private Image CropAsSquare(Image img)
        {
            if (img.Width == img.Height) return img;
            Bitmap tmpBitmap = new Bitmap(img);
            Bitmap bMap = (Bitmap)tmpBitmap.Clone();
            int size = (img.Width > img.Height) ? img.Height : img.Width;
            int xPosition = 0, yPosition = 0;
            if (img.Width > img.Height)
            {
                xPosition = (img.Width - size) / 2;
                yPosition = 0;
            }
            else
            {
                yPosition = (img.Height - size) / 2;
                xPosition = 0;
            }
            Rectangle rect = new Rectangle(xPosition, yPosition, size, size);
            tmpBitmap = bMap.Clone(rect, bMap.PixelFormat);
            MemoryStream stream = new MemoryStream();
            tmpBitmap.Save(stream, ImageFormat.Jpeg);
            return Image.FromStream(stream);
        }

        private Dictionary<string,TwitterUser> GetRetweetedUsers(string screenName)
        {
            Dictionary<string, TwitterUser> users = new Dictionary<string, TwitterUser>();
            var config = new System.Configuration.AppSettingsReader();
            var auth = new ApplicationOnlyAuthorizer()
            {
                CredentialStore =
                    new InMemoryCredentialStore()

                    {
                        ConsumerKey = ConfigurationManager.AppSettings["ConsumerKey"],
                        ConsumerSecret = ConfigurationManager.AppSettings["ConsumerSecret"]
                    }
            };
            var task = auth.AuthorizeAsync();
            task.Wait();

            var twx = new TwitterContext(auth);

            var tweets = from tweet in twx.Status
                         where tweet.Type == StatusType.User &&
                               tweet.ScreenName == screenName &&
                               tweet.Count == 300 &&
                               tweet.TrimUser == false &&
                               tweet.ExcludeReplies == false
                         select tweet;
            foreach (var tweet in tweets)
            {
                if (tweet.RetweetedStatus.User != null)
                {
                    var screenNameRt = tweet.RetweetedStatus.User.ScreenNameResponse.Trim();
                    if (users.ContainsKey(screenNameRt))
                    {
                        users[screenNameRt].RetweetsCount++;
                    }
                    else
                    {
                        users.Add(screenNameRt, new TwitterUser()
                        {
                            ScreenName = screenNameRt,
                            ImageUrl = tweet.RetweetedStatus.User.ProfileImageUrl.Remove(tweet.RetweetedStatus.User.ProfileImageUrl.LastIndexOf("_normal",StringComparison.Ordinal),7),
                            RetweetsCount = 1
                        });
                    }
                }
            }
            return users;
        }
    }
}