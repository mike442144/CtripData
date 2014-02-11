using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HtmlAgilityPack;
using PinYin;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using System.Threading;

namespace CtripData
{
    class Program
    {
        public void ReadCitiesFromFile()
        {

        }
        static string GetUrl()
        {
            string url = urlHost + urlPath + "p"+curPageNum;
            Console.WriteLine("Getting from " + url);
            return url;
        }
        static string GetHotelUrl(Hotel h)
        {
            return urlHost + h.UrlPath;
        }
        static string GetHotelCommentUrl(Hotel h)
        {
            return urlHost + "Domestic/tool/AjaxGetHotelDetailComment.aspx?hotel=" + h.UnitId;
        }
        static HtmlNode GetHotelDocRoot(Hotel h,string type="hotel")
        {
            string url = string.Empty;
            if(type=="comment")
                url = GetHotelCommentUrl(h);
            else url = GetHotelUrl(h);
            var req = HttpWebRequest.Create(url) as HttpWebRequest;
            req.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
            req.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/32.0.1700.107 Safari/537.36";
            req.Headers.Add("Accept-Encoding", "gzip,deflate,sdch");
            req.Headers.Add("Accept-Language", "zh-CN,zh;q=0.8,en;q=0.6");
            string ip;
            if (httpGetCount % 50 == 0)
            {
                ip = IPGen.NextIP();
                Console.WriteLine("Change IP to "+ip);
            }
            else
                ip = IPGen.CurIP();
            req.Headers.Add("X_FORWARDED_FOR", ip);
            req.Host = "hotels.ctrip.com";
            httpGetCount++;
            try
            {
                var res = req.GetResponse() as HttpWebResponse;
                var doc = new HtmlDocument();
                using (Stream stream1 = GzipConverter.Gzip(res))
                {
                    using (StreamReader sr = new StreamReader(stream1, Encoding.GetEncoding(res.ContentType.Replace("text/html;", string.Empty).Replace("charset=", string.Empty).Trim())))
                    {
                        var content = sr.ReadToEnd();
                        //File.WriteAllText(Guid.NewGuid() + ".html", content);
                        doc.LoadHtml(content);
                    }
                }
                return doc.DocumentNode;
            }
            catch (System.Net.WebException error)
            {
                Console.WriteLine(error.Message);
            }
            return null;
        }
        static HtmlNode GetDocRoot()
        {
            try
            {
                var res = RequestPost.PostDataToUrlW(urlQuery,GetUrl()) as HttpWebResponse;
                if (res == null) return null;
                var doc = new HtmlDocument();
                using (Stream stream1 = GzipConverter.Gzip(res))
                {
                    using (StreamReader sr = new StreamReader(stream1, Encoding.GetEncoding(res.ContentType.Replace("text/html;",string.Empty).Replace("charset=",string.Empty).Trim())))
                    {
                        var content = sr.ReadToEnd();
                        //File.WriteAllText(Guid.NewGuid() + ".html", content);
                        doc.LoadHtml(content);
                    }
                }
                return doc.DocumentNode;
            }
            catch (System.Net.WebException error)
            {
                Console.WriteLine(error.Message);
            }
            return null;
        }
        static string urlPath = string.Empty;
        static string urlHost = "http://hotels.ctrip.com/";
        static string urlQuery = "StartTime=2014-02-11&DepTime=2014-02-12";
        
        static int curPageNum = 1;
        static List<Hotel> hotels = new List<Hotel>();
        static List<string> unitBasicInfoKeys = new List<string>() { "房屋类型", "房屋面积", "可住人数", "卧室数", "房屋套数", "床数", "床型", "卫生间数", "最少天数", "最多天数", "发票", "接待外宾" };
        static Dictionary<string, string> py2Han = new Dictionary<string, string>();

        static Dictionary<int, int> priceDict = new Dictionary<int, int>();
        static HashSet<int> gotHotels;
        static int httpGetCount = 0;

        static void InitPriceDict()
        {
            priceDict.Add(2,1);
            priceDict.Add(0, 2);
            priceDict.Add(5, 3);
            priceDict.Add(9, 4);
            priceDict.Add(3, 5);
            priceDict.Add(1, 6);
            priceDict.Add(8, 7);
            priceDict.Add(6, 8);
            priceDict.Add(4, 9);
            priceDict.Add(7, 0);
        }
        static void InitServiceDict()
        {
            Hotel.Services.Add("ico_wifi", "公共区域WIFI");
            Hotel.Services.Add("ico_parking", "停车场");
            Hotel.Services.Add("ico_swim", "游泳池");
            Hotel.Services.Add("ico_gym", "健身房");
            Hotel.Services.Add("ico_meeting", "会议室");
            Hotel.Services.Add("ico_breakfast", "餐厅");
            Hotel.Services.Add("ico_airport_shuttle", "接机服务");
            Hotel.Services.Add("ico_free_wifi", "免费公共区域WIFI");
            Hotel.Services.Add("ico_bus", "穿梭机场班车");
        }
        static void LoadAllIP()
        {
            IPGen.FromFile("ip.txt");
        }
        static void LoadGotHotels()
        {
            gotHotels = new HashSet<int>();
            if (!File.Exists(gotHotelsFile))
                return;
            using (var sr = new StreamReader(gotHotelsFile))
            {
                while (!sr.EndOfStream)
                {
                    var id = Convert.ToInt32(sr.ReadLine());
                    if (!gotHotels.Contains(id))
                        gotHotels.Add(id);
                }
            }
        }
        static void Main(string[] args)
        {
            InitPriceDict();
            InitServiceDict();
            LoadAllIP();
            LoadGotHotels();
            var cityList = new List<string>();
            using (var fs = File.OpenRead("TextFile1.txt"))
            {
                var sr = new StreamReader(fs);
                string c = string.Empty;
                while (!string.IsNullOrWhiteSpace(c = sr.ReadLine()))
                {
                    var vals = c.Split(' ');
                    if(!py2Han.ContainsKey(vals[0]))
                        py2Han.Add(vals[0], vals[1]);
                    cityList.Add(vals[0]);
                }
            }

            foreach (string city in cityList)
            {
                curPageNum = 1;
                urlPath = "hotel/"+city + "/star3star4star5";
                var root = GetDocRoot();
                if (root == null) continue;
                //验证城市是否存在
                var wrapNodes = root.SelectNodes("//strong[@class='error']");
                if (wrapNodes != null)
                {
                    Console.WriteLine("没有找到城市");
                    continue;
                }

                //验证是否有满足条件的记录
                //var record = root.SelectSingleNode("//div[@id='noRecord']");
                //if (record != null)
                //{
                //    Console.WriteLine("没有符合条件的记录");
                //    continue;
                //}

                var lastPageNode = root.SelectSingleNode("//div[@class='c_page_list layoutfix']/a[last()]");
                var pageCount = 1;
                if (lastPageNode != null)
                {
                    try
                    {
                        pageCount = Int32.Parse(lastPageNode.InnerText);
                    }
                    catch
                    {
                        continue;
                    }
                }
                //pageCount = 1;
                while (curPageNum <= pageCount)
                {
                    if (root == null)
                        root = GetDocRoot();
                    if (root != null)
                    {
                        var hotelNodes = root.SelectNodes("//div[@class='searchresult_list']");
                        if (hotelNodes != null)
                        {
                            foreach (HtmlNode hotel in hotelNodes)
                            {
                                int id = Convert.ToInt32(hotel.Attributes["id"].Value);
                                if (gotHotels.Contains(id)) continue;
                                var h = new Hotel();
                                h.UnitId = id;
                                
                                var hotelLink = hotel.SelectSingleNode("ul/li/h2/a");
                                
                                if (hotelLink != null)
                                {
                                    h.UrlPath = "hotel/"+h.UnitId+".html";
                                    h.Name = hotelLink.Attributes["title"].Value;
                                    var starNode = hotel.SelectSingleNode("ul/li/p[@class='medal_list']/span");
                                    if (starNode != null)
                                    {
                                        h.Star = starNode.Attributes["title"].Value;
                                    }
                                }
                                //if (selfNode != null)
                                //    h.TujiaSelf = true;
                                h.CityName = py2Han[city];
                                var roomNodes = hotel.SelectNodes("div[@class='room_list2']/table/tbody/tr");
                                if (roomNodes != null && roomNodes.Count > 0)
                                {
                                    var roomList = new List<Room>();
                                    foreach (HtmlNode roomNode in roomNodes)
                                    {
                                        var roomProperties = roomNode.SelectNodes("td");
                                        
                                        if (roomProperties != null)
                                        {
                                            var room = new Room();
                                            //name and value
                                            var roomLink = roomProperties[0].SelectSingleNode("a[@class='hotel_room_name']");
                                            if (roomLink != null)
                                            {
                                                room.Name = roomLink.Attributes["title"].Value;
                                            }
                                            var roomText = roomProperties[0].SelectSingleNode("a[@class='hotel_room_text']");
                                            if (roomText != null)
                                            {
                                                room.Text = roomText.InnerText;
                                            }
                                            //var v = roomProperties[0].SelectSingleNode("input[@type='hidden']");
                                            //if (v != null)
                                            //{
                                            //    room.Value = Convert.ToInt32(v.Attributes["value"].Value);
                                            //}

                                            //bed type
                                            var bedNode = roomProperties[1].SelectSingleNode("span");
                                            if (bedNode != null)
                                            {
                                                room.BedType = bedNode.InnerText;
                                            }
                                            var breakfastNode = roomProperties[2].SelectSingleNode("span");
                                            if(breakfastNode!=null)
                                                room.Breakfast = breakfastNode.InnerText;
                                            var networkNode = roomProperties[3].SelectSingleNode("span");
                                            if (networkNode != null)
                                                room.Network = networkNode.InnerText;
                                            var policyNode = roomProperties[4].SelectSingleNode("span");
                                            if (policyNode != null)
                                                room.Policy = policyNode.InnerText;
                                            var unitNode = roomProperties[5].SelectSingleNode("span/dfn");
                                            string priceUnit=string.Empty;
                                            if (unitNode != null)
                                            {
                                                priceUnit = unitNode.InnerText;
                                            }
                                            var priceNodes = roomProperties[5].SelectNodes("span/var");
                                            if (priceNodes != null)
                                            {
                                                int p = 0;
                                                foreach (HtmlNode pn in priceNodes)
                                                {
                                                    int val = Convert.ToInt32(pn.Attributes["class"].Value.Replace("p_h57_", string.Empty));
                                                    val = priceDict[val];
                                                    p = p * 10 + val;
                                                }

                                                room.Price = HttpUtility.HtmlDecode(priceUnit) +" " + p;
                                            }
                                            var promotionNode = roomProperties[6].SelectSingleNode("span");
                                            if (promotionNode != null)
                                            {
                                                var proTxt = promotionNode.InnerText.Replace("<i>",string.Empty).Replace("</i>",string.Empty);
                                                room.CutDown = proTxt;
                                            }
                                            var ps = roomProperties[7].SelectNodes("span");
                                            if (ps != null)
                                            {
                                                foreach (HtmlNode p in ps)
                                                {
                                                    if ("icon_prepay" == p.Attributes["class"].Value)//预付
                                                    {
                                                        room.Prepay = true;
                                                    }
                                                    else if ("ico_vouch" == p.Attributes["class"].Value)//担保
                                                    {
                                                        room.Vouch = true;
                                                    }
                                                }
                                            }
                                            roomList.Add(room);
                                        }
                                    }
                                    h.Rooms = roomList;
                                }
                                hotels.Add(h);
                            }
                            root = null;
                        }
                    }
                    curPageNum++;
                }
            }
            Console.WriteLine("There are total " + hotels.Count + " hotels.");
            int i = 1;
            foreach (Hotel h in hotels)
            {
                Console.WriteLine("Getting data " + i++ + "/" + hotels.Count + "...");
                var root = GetHotelDocRoot(h);
                if (root == null) continue;
                //公寓基本信息
                //var rows = root.SelectNodes("//div[@id='unitBasicInfo']/table/tbody/tr");
                //if (rows != null && rows.Count > 0)
                //{
                //    var kvs = new NameValueCollection();
                //    foreach (HtmlNode row in rows)
                //    {
                //        var kvNodes = row.SelectNodes("td");
                //        if (kvNodes != null && kvNodes.Count == 2)
                //        {
                //            var k = kvNodes[0].InnerText;
                //            if (!string.IsNullOrWhiteSpace(k))
                //                k = k.Trim();
                //            if (!unitBasicInfoKeys.Contains(k)) continue;
                //            var v = kvNodes[1].InnerText;
                //            if (!string.IsNullOrWhiteSpace(v))
                //                v = v.Trim();
                //            kvs.Add(k, v);
                //        }
                //    }
                //    h.UnitBasicInfo = kvs;
                //}
                //小区基本信息
                //var birows = root.SelectNodes("//div[@id='basicInfo']/table/tbody/tr");
                //if (birows != null && birows.Count > 0)
                //{
                //    var bikvs = new NameValueCollection();
                //    foreach (HtmlNode r in birows)
                //    {
                //        var bikvNodes = r.SelectNodes("td");
                //        if (bikvNodes != null && bikvNodes.Count == 2)
                //        {
                //            var k = bikvNodes[0].InnerText;
                //            k = k.Trim();
                //            if (!string.IsNullOrWhiteSpace(k) && k.EndsWith("："))
                //                k = k.Substring(0, k.Length - 1);
                //            var v = bikvNodes[1].InnerText;
                //            v = v.Trim();
                //            bikvs.Add(k, v);
                //        }
                //    }
                //    h.BasicInfo = bikvs;
                //}
                //速订，24小时可退，实拍
                //var serviceNodes = root.SelectNodes("//div[@class='house-services']/span");
                //if (serviceNodes != null)
                //{
                //    foreach (HtmlNode item in serviceNodes)
                //    {
                //        var t = item.Attributes["class"].Value.Split(' ')[0];
                //        if (t == "suding")
                //            h.Suding = true;
                //        else if (t == "tuikuan")
                //            h.Tuikuan = true;
                //        else if (t == "shipia")
                //            h.Shipai = true;
                //    }
                //}
                //评价信息
                var houseRatingNode = root.SelectSingleNode("//div[@class='htl_com ']/a[@id='LinkReview2']/span[@class='score']");
                if (houseRatingNode != null)
                {
                    h.Rating = houseRatingNode.InnerText;
                }
                var ccNode = root.SelectSingleNode("//div[@class='htl_com ']/a[@id='LinkReview2']/span[@class='commnet_num']/span");
                if (ccNode != null)
                {
                    var ccTxt = ccNode.InnerText;
                    if (!string.IsNullOrWhiteSpace(ccTxt))
                    {
                        h.CommentCount = ccTxt.Trim();
                    }
                }
                //var bookNode = root.SelectSingleNode("//div[@class='house-info-item']/div[@class='house-booking-count']/span/strong");
                //if (bookNode != null)
                //{
                //    try
                //    {
                //        var strNum = bookNode.InnerText;
                //        if (!string.IsNullOrWhiteSpace(strNum))
                //        {
                //            h.BookingCount = Convert.ToInt32(strNum.Trim());
                //        }

                //    }
                //    catch (FormatException)
                //    {
                //        h.BookingCount = 0;
                //    }
                //}
                var picNode = root.SelectSingleNode("//div[@id='topPicList']/p/a");
                if (picNode != null)
                {
                    var t = picNode.InnerText;
                    if (!string.IsNullOrWhiteSpace(t))
                    {
                        var match = Regex.Match(t, @"\d+");
                        try
                        {
                            h.PicCount = Convert.ToInt32(match.Value);
                        }
                        catch
                        {
                            Console.WriteLine("获取图片数失败.");
                        }
                    }
                }
                var tagNodes = root.SelectNodes("//div[@class='icon_list']/span");
                if (tagNodes != null)
                {
                    foreach (HtmlNode t in tagNodes)
                    {
                        if (t.Attributes["class"] != null)
                        {
                            string name = t.Attributes["class"].Value;
                            string text = t.Attributes["title"].Value;
                            if ("ico_free_wifi" == name)
                                text = "免费" + text;
                            if (!h.ServiceProvided.ContainsKey(name))
                                h.ServiceProvided.Add(name, text);
                            
                            //if (!Hotel.Services.ContainsKey(name))
                            //    Hotel.Services.Add(name, text);
                        }
                    }
                }
                var commentRoot = GetHotelDocRoot(h, "comment");
                var cmtItems = commentRoot.SelectNodes("//div[@class='comment_overall_s_item']/div[@class='item_title']");
                if (cmtItems != null)
                {
                    foreach (var item in cmtItems)
                    {
                        int item_key = Convert.ToInt32(item.FirstChild.Attributes["value"].Value);
                        string item_val = HttpUtility.HtmlDecode(item.LastChild.InnerText).Trim();
                        if (!h.RatingDetails.ContainsKey(item_key))
                            h.RatingDetails.Add(item_key, item_val);
                    }
                }
                File.AppendAllLines(resultFile, h.ToString());
                File.AppendAllLines(gotHotelsFile, new List<string>(){h.UnitId.ToString()});
                //Random r = new Random();
                //Thread.Sleep(r.Next(500, 10000));
            }
            //File.WriteAllText("tags.txt", Hotel.Services.Select(kv => kv.Value).Aggregate((x, y) => x + " " + y));
            //i = 1;
            //foreach (var hotel in hotels)
            //{
            //    Console.WriteLine("Writing file "+i+++"/"+hotels.Count);
            //    File.AppendAllLines(resultFile, hotel.ToString());
            //}
        }
        static string resultFile = "data.txt";
        static string gotHotelsFile = "got.txt";
    }
    class Hotel
    {
        public Hotel()
        {
            UnitBasicInfo = new NameValueCollection();
            Tags = new NameValueCollection();
            BasicInfo = new NameValueCollection();
        }
        public static Dictionary<string, string> Services = new Dictionary<string, string>();
        public string Name;
        public bool TujiaSelf = false;
        public int UnitId;
        public string UrlPath;
        public string CityName;
        public NameValueCollection UnitBasicInfo, Tags, BasicInfo;
        public int Price;
        public string CutDown = "无";
        public bool Suding, Shipai, Tuikuan;
        public string Rating = "0";
        public Dictionary<int, string> RatingDetails = new Dictionary<int, string>(4);
        public string CommentCount = "0";
        public int BookingCount = 0;
        public List<Room> Rooms;
        public int PicCount;
        public string Star;
        public Dictionary<string, string> ServiceProvided = new Dictionary<string, string>();
        public IEnumerable<String> ToString()
        {
            var result = new List<String>();
            StringBuilder sb = new StringBuilder();
            if (Rooms == null)
                return result;
            foreach (var room in Rooms)
            {
                sb.Clear();
                sb.Append(this.CityName);
                sb.Append(",");
                sb.Append(this.Name);
                sb.Append(",");
                sb.Append(this.Star);
                sb.Append(",");
                sb.Append(room.Name);
                sb.Append(",");
                sb.Append(room.BedType);
                sb.Append(",");
                sb.Append(room.Price);
                sb.Append(",");
                sb.Append(room.CutDown);
                sb.Append(",");
                
                sb.Append(this.Rating);
                sb.Append(",");
                sb.Append(
                    this.RatingDetails.ContainsKey(1)?RatingDetails[1]:string.Empty
                    );
                sb.Append(",");
                sb.Append(
                    this.RatingDetails.ContainsKey(2) ? RatingDetails[2] : string.Empty
                    );
                sb.Append(",");
                sb.Append(
                    this.RatingDetails.ContainsKey(3) ? RatingDetails[3] : string.Empty
                    );
                sb.Append(",");
                sb.Append(
                    this.RatingDetails.ContainsKey(6) ? RatingDetails[6] : string.Empty
                    );
                sb.Append(",");
                
                sb.Append(room.Network);
                sb.Append(",");
                sb.Append(this.CommentCount);
                sb.Append(",");
                sb.Append(this.PicCount);
                sb.Append(",");
                sb.Append(room.Breakfast);
                sb.Append(",");
                sb.Append(room.Policy);
                sb.Append(",");
                sb.Append(room.Prepay?"是":"否");
                sb.Append(",");
                sb.Append(room.Vouch ? "是" : "否");
                sb.Append(",");
                foreach (string k in Services.Keys)
                {
                    if (this.ServiceProvided.ContainsKey(k))
                        sb.Append("是");
                    else
                        sb.Append("否");
                    sb.Append(",");
                }
                sb.Append(" ");
                result.Add(sb.ToString());
            }
            return result;
        }
    }
    class Room
    {
        public string Pic;
        public string Name;
        public string Text;
        public int Value;
        public string BedType;
        public string Breakfast;
        public string Network;
        public string Policy;
        public string Price;
        public string CutDown;
        public bool Prepay = false;
        public bool Vouch = false;
        public override string ToString()
        {
            return base.ToString();
        }
    }
}
