using System.Net;

namespace IntelioAPI
{
    public class TelegramBot
    {
        public TelegramBot() {  }

        public void SendTextMessage(string Text)
        {
            string botToken = "6645316932:AAE714vfzDOV21hs585fcLjpEMkL9kQZaW8";


            try
            {
                string url = $"https://api.telegram.org/bot{botToken}/sendMessage?chat_id=-1002144477508&parse_mode=HTML&text={Text}";

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    using (Stream dataStream = response.GetResponseStream())
                    {
                        StreamReader reader = new StreamReader(dataStream);
                        string responseFromServer = reader.ReadToEnd();
                        //Console.WriteLine(responseFromServer);
                    }
                }
            } catch (Exception ex)
            {
                string url = $"https://api.telegram.org/bot{botToken}/sendMessage?chat_id=-1002144477508&text={Text}";

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    using (Stream dataStream = response.GetResponseStream())
                    {
                        StreamReader reader = new StreamReader(dataStream);
                        string responseFromServer = reader.ReadToEnd();
                        //Console.WriteLine(responseFromServer);
                    }
                }
            }
        }
    }
}
