using HtmlAgilityPack;
using System.Formats.Asn1;
using System.Net;

namespace IntelioAPI
{
    public class GetPicture
    {
        public string getPictureUrl(string url)
        {
            HtmlWeb web = new HtmlWeb();
            HtmlDocument doc = web.Load(url);

            var metaTags = doc.DocumentNode.SelectNodes("//meta");

            string imageUrl = "";

            try
            {
                foreach (var tag in metaTags)
                {
                    var property = tag.GetAttributeValue("property", "");
                    if (property == "og:image")
                    {
                        imageUrl = tag.GetAttributeValue("content", "");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return "https://i.ibb.co/sWHtCbW/Intelio.png";
            }

            if (!string.IsNullOrEmpty(imageUrl))
            {
                return imageUrl;
            }
            else
            {
                return "https://i.ibb.co/sWHtCbW/Intelio.png";
            }
        }
    }
}
