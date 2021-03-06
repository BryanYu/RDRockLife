﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Http;
using System.Xml.XPath;

using HtmlAgilityPack;

using isRock.LineBot;

namespace RDRockLife.Controllers
{
    public class LineController : ApiController
    {
        private readonly string _channelToken = ConfigurationManager.AppSettings["ChannelToken"].ToString();

        [HttpPost]
        public IHttpActionResult Post()
        {
            var replyToken = string.Empty;
            try
            {
                string postData = Request.Content.ReadAsStringAsync().Result;
                var receivedMessage = isRock.LineBot.Utility.Parsing(postData);
                replyToken = receivedMessage.events[0].replyToken;
                var message = GetMessage(receivedMessage);
                if (!string.IsNullOrEmpty(message))
                {
                    isRock.LineBot.Utility.ReplyMessage(replyToken, message, this._channelToken);
                }
                return Ok();
            }
            catch (Exception ex)
            {
                isRock.LineBot.Utility.ReplyMessage(
                    replyToken,
                    Newtonsoft.Json.JsonConvert.SerializeObject(ex),
                    this._channelToken);
                return Ok();
            }
        }

        [HttpGet]
        public IHttpActionResult Get(int stockId)
        {
            try
            {
                var result = this.GetStock(stockId);
                return this.Ok(stockId);
            }
            catch (Exception e)
            {
                return this.Ok(e);
            }
        }

        private string GetMessage(ReceievedMessage receivedMessage)
        {
            if (receivedMessage.events[0].type == "join")
            {
                return "歡迎來到布萊恩RD滾石人生頻道";
            }

            //if (receivedMessage.events[0].type == "message" && receivedMessage.events[0].source.userId
            //    == "Ucb61a0292a1972e531f8bd123a7dca02")
            //{
            //    return "林貝珊再吵打你屁股";
            //}
            //if (receivedMessage.events[0].type == "message" && receivedMessage.events[0].source.userId == "U142ea503c7835a515b3ad34db5506f1f")
            //{
            //    return "謝宗明不要再鴨講了";
            //}

            //if (receivedMessage.events[0].type == "message" && receivedMessage.events[0].source.userId == "Ud0b4e4ce86825b1621b960741726223a")
            //{
            //    return "阿信就是胡瓜";
            //}

            //if (receivedMessage.events[0].type == "message" && receivedMessage.events[0].source.userId == "Ub3f1e20be96bd82b11192c613768423c")
            //{
            //    return "劉醬瓜不要再遲到了";
            //}

            var messageText = receivedMessage.events[0].message.text;

            //if (receivedMessage.events[0].type == "message" && messageText.Contains("五月天"))
            //{
            //    return "浪費錢";
            //}
            //if (receivedMessage.events[0].type == "message" && messageText.Contains("阿信"))
            //{
            //    return "胡瓜";
            //}
            if (receivedMessage.events[0].type == "message" && messageText.Contains("股票"))
            {
                int stockId = 0;
                bool isStockId = int.TryParse(messageText.Replace("股票", string.Empty).Trim(), out stockId);
                if (isStockId)
                {
                    return GetStock(stockId);
                }
            }
            return string.Empty;
        }

        private string GetStock(int stockId)
        {
            //指定來源網頁
            WebClient url = new WebClient();
            MemoryStream ms = new MemoryStream(url.DownloadData("http://tw.stock.yahoo.com/q/q?s=" + stockId));
            HtmlDocument doc = new HtmlDocument();
            doc.Load(ms, Encoding.GetEncoding("big5"));
            HtmlDocument hdc = new HtmlDocument();
            hdc.LoadHtml(
                doc.DocumentNode.SelectSingleNode("/html[1]/body[1]/center[1]/table[2]/tr[1]/td[1]/table[1]")
                    .InnerHtml);

            // 取得個股標頭
            HtmlNodeCollection htnode = hdc.DocumentNode.SelectNodes("./tr[1]/th");
            htnode.Remove(htnode.FirstOrDefault(item => item.InnerText == "個股資料"));
            // 取得個股數值
            string[] txt = hdc.DocumentNode.SelectSingleNode("./tr[2]").InnerText.Replace("加到投資組合", string.Empty)
                .Trim().Split('\n');
            int i = 0;
            var result = new StringBuilder();

            foreach (HtmlNode nodeHeader in htnode)
            {
                var title = i == 0 ? string.Empty : nodeHeader.InnerText + ":";
                result.Append($"{title}{txt[i]} \n");
                i++;
            }

            doc = null;
            hdc = null;
            url = null;
            ms.Close();
            return HttpUtility.HtmlDecode(result.ToString());
        }
    }
}