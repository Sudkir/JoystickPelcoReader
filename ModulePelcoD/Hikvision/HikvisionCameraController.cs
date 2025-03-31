//using System;
//using System.IO;
//using System.Linq;
//using System.Net;
//using System.Text;
//using System.Threading.Tasks;
//using System.Xml.Linq;
//using UnityEngine;

//namespace ModulePelcoD.Hikvision
//{
//    public static class HikvisionCameraController
//    {
//        private static string user;
//        private static string User => user ??= AppConfig.Get("HikvisionCameraUserName", "admin");
//        private static string password;
//        private static string Password => password ??= AppConfig.Get("HikvisionCameraPassword", "VirSign2022");
//        private static int? requestTimeout;
//        private static int RequestTimeout => requestTimeout ?? (requestTimeout = AppConfig.Get("HikvisionKameraRequestTimeoutMs", 5000)).Value;

//        public static CredentialCache GetCredentialCache(Uri uri)
//        {
//            return new CredentialCache
//        {
//            {
//                new Uri(uri.GetLeftPart(UriPartial.Authority)), // request url's host
//                "Digest",  // authentication type 
//                new NetworkCredential(User, Password) // credentials 
//            }
//        };
//        }

//        public static async Task SetMode(string mode, string ip, Action onError)
//        {
//            var postBody = $@"<?xml version=""1.0"" encoding=""UTF-8""?><MountingScenario><mode>{mode}</mode></MountingScenario>";

//            var url = $"http://{ip}/ISAPI/Image/channels/1/mountingScenario";
//            var uri = new Uri(url);
//            var request = WebRequest.CreateHttp(uri);
//            request.Timeout = RequestTimeout;
//            request.Method = "PUT";
//            request.ContentType = "application/xml";
//            request.Credentials = GetCredentialCache(uri);
//            try
//            {
//                var bytes = Encoding.UTF8.GetBytes(postBody);
//                using var newStream = await request.GetRequestStreamAsync();
//                await newStream.WriteAsync(bytes, 0, bytes.Length);
//                using var response = (await request.GetResponseAsync()) as HttpWebResponse;
//                if (response.StatusCode != HttpStatusCode.OK)
//                {
//                    onError.Invoke();
//                }
//            }
//            catch
//            {
//                onError.Invoke();
//            }
//        }

//        public static async Task<string> GetCurrentMode(string ip, Action onError)
//        {
//            var mode = string.Empty;
//            var url = $"http://{ip}/ISAPI/Image/channels/1/mountingScenario/capabilities";
//            var uri = new Uri(url);
//            var request = WebRequest.CreateHttp(uri);
//            request.Timeout = RequestTimeout;
//            request.Method = "GET";
//            request.ContentType = "application/xml";
//            request.Credentials = GetCredentialCache(uri);
//            try
//            {
//                using var response = (await request.GetResponseAsync()) as HttpWebResponse;
//                if (response.StatusCode == HttpStatusCode.OK)
//                {
//                    using var responseStream = response.GetResponseStream();
//                    using var reader = new StreamReader(responseStream, Encoding.UTF8);
//                    var responseText = await reader.ReadToEndAsync();

//                    var xDoc = XDocument.Parse(responseText);
//                    XNamespace ns = "http://www.hikvision.com/ver20/XMLSchema";
//                    mode = xDoc.Descendants(ns + "mode").FirstOrDefault().Value;
//                }
//                else
//                    onError.Invoke();
//            }
//            catch
//            {
//                onError.Invoke();
//            }
//            return mode;
//        }

//        public static async Task<Vector2Int> GetCurrentResolution(string ip, Action onError)
//        {
//            var resolution = Vector2Int.zero;
//            var url = $"http://{ip}/ISAPI/Streaming/channels/101";
//            var uri = new Uri(url);
//            var request = WebRequest.CreateHttp(uri);
//            request.Timeout = RequestTimeout;
//            request.Method = "GET";
//            request.ContentType = "application/xml";
//            request.Credentials = GetCredentialCache(uri);
//            try
//            {
//                using var response = (await request.GetResponseAsync()) as HttpWebResponse;
//                if (response.StatusCode == HttpStatusCode.OK)
//                {
//                    using var responseStream = response.GetResponseStream();
//                    using var reader = new StreamReader(responseStream, Encoding.UTF8);
//                    var responseText = await reader.ReadToEndAsync();

//                    var xDoc = XDocument.Parse(responseText);
//                    XNamespace ns = "http://www.hikvision.com/ver20/XMLSchema";
//                    var width = xDoc.Descendants(ns + "videoResolutionWidth").FirstOrDefault().Value;
//                    var height = xDoc.Descendants(ns + "videoResolutionHeight").FirstOrDefault().Value;
//                    resolution = new Vector2Int(int.Parse(width), int.Parse(height));
//                }
//                else
//                {
//                    onError.Invoke();
//                }
//            }
//            catch
//            {
//                onError.Invoke();
//            }
//            return resolution;
//        }

//        public static async Task SetResolution(Vector2Int resolution, string ip, Action onError)
//        {
//            var channelId = 101;
//            var url = $"http://{ip}/ISAPI/Streaming/channels/{channelId}";

//            var requestBody = GetStreamVideoConfigurationRequestBody(resolution, channelId);
//            var bytes = Encoding.UTF8.GetBytes(requestBody);
//            var uri = new Uri(url);
//            var request = WebRequest.Create(uri);
//            request.Timeout = RequestTimeout;
//            request.Method = "PUT";
//            request.ContentType = "aapplication/x-www-form-urlencoded; charset=UTF-8";
//            request.ContentLength = bytes.Length;
//            request.Credentials = GetCredentialCache(uri);
//            try
//            {
//                using var newStream = await request.GetRequestStreamAsync();
//                await newStream.WriteAsync(bytes, 0, bytes.Length);
//                using var response = await request.GetResponseAsync() as HttpWebResponse;

//                if (response.StatusCode != HttpStatusCode.OK)
//                {
//                    onError.Invoke();
//                }
//            }
//            catch
//            {
//                onError.Invoke();
//            }
//        }

//        public static async Task SetCameraVideoSettings(Vector2Int resolution, int framerate, bool isHighBitrate, string ip, Action onError)
//        {
//            var channelId = 101;
//            var url = $"http://{ip}/ISAPI/Streaming/channels/{channelId}";
//            var requestBody = SetCameraVideoSettingsPayload(resolution, framerate, isHighBitrate, channelId);
//            var bytes = Encoding.UTF8.GetBytes(requestBody);
//            var uri = new Uri(url);
//            var request = WebRequest.Create(uri);
//            request.Timeout = RequestTimeout;
//            request.Method = "PUT";
//            request.ContentType = "aapplication/x-www-form-urlencoded; charset=UTF-8";
//            request.ContentLength = bytes.Length;
//            request.Credentials = GetCredentialCache(uri);/* new CredentialCache
//        {
//            {
//                new Uri(uri.GetLeftPart(UriPartial.Authority)), // request url's host
//                "Digest",  // authentication type
//                new NetworkCredential("admin", "VirSign2022") // credentials
//            }
//        };*/
//            try
//            {
//                using var newStream = await request.GetRequestStreamAsync();
//                await newStream.WriteAsync(bytes, 0, bytes.Length);
//                using var response = await request.GetResponseAsync() as HttpWebResponse;

//                if (response.StatusCode != HttpStatusCode.OK)
//                {
//                    onError.Invoke();
//                }
//            }
//            catch (Exception e)
//            {
//                onError.Invoke();
//            }
//        }



//        public static string CreateMD5(string input)
//        {
//            using var md5 = System.Security.Cryptography.MD5.Create();
//            var inputBytes = Encoding.ASCII.GetBytes(input);
//            var hashBytes = md5.ComputeHash(inputBytes);

//            var sb = new StringBuilder();
//            for (var i = 0; i < hashBytes.Length; i++)
//            {
//                _ = sb.Append(hashBytes[i].ToString("X2"));
//            }
//            return sb.ToString();
//        }

//        private static string GetStreamVideoConfigurationRequestBody(Vector2Int resolution, int channelId)
//        {
//            return @$"
//""1.0"" encoding=""UTF-8""?>
//<StreamingChannel
//	xmlns=""http://www.hikvision.com/ver20/XMLSchema"" version=""2.0"">
//	<id>{channelId}</id>
//	<channelName>Camera {channelId}</channelName>
//	<enabled>true</enabled>
//	<Video
//		xmlns="""">
//		<enabled>true</enabled>
//		<videoInputChannelID>1</videoInputChannelID>
//		<videoCodecType>H.264</videoCodecType>
//		<videoResolutionWidth>{resolution.x}</videoResolutionWidth>
//		<videoScanType>progressive</videoScanType>
//		<videoResolutionHeight>{resolution.y}</videoResolutionHeight>
//		<videoQualityControlType>cbr</videoQualityControlType>
//		<constantBitRate>4096</constantBitRate>
//		<maxFrameRate>2500</maxFrameRate>
//		<GovLength>25</GovLength>
//		<H264Profile>Main</H264Profile>
//		<SVC>
//			<enabled>false</enabled>
//		</SVC>
//		<smoothing>50</smoothing>
//		<SmartCodec>
//			<enabled>false</enabled>
//		</SmartCodec>
//	</Video>
//</StreamingChannel>";
//        }

//        private static string SetCameraVideoSettingsPayload(
//            Vector2Int resolution,
//            int framerate,
//            bool isHighBitrate,
//            int channelId) =>
//    @$"<?xml version=""1.0"" encoding=""UTF-8""?>
//<StreamingChannel
//  xmlns=""http://www.hikvision.com/ver20/XMLSchema"" version=""2.0"">
//  <id>{channelId}</id>
//  <channelName>Camera {channelId}</channelName>
//  <enabled>true</enabled>
//  <Video xmlns="""">
//    <enabled>true</enabled>
//    <videoInputChannelID>1</videoInputChannelID>
//    <videoCodecType>H.265</videoCodecType>
//    <videoResolutionWidth>{resolution.x}</videoResolutionWidth>
//    <videoScanType>progressive</videoScanType>
//    <videoResolutionHeight>{resolution.y}</videoResolutionHeight>
//    <videoQualityControlType>cbr</videoQualityControlType>
//    <constantBitRate>{(isHighBitrate ? "4096" : "2048")}</constantBitRate>
//    <maxFrameRate>{framerate * 100}</maxFrameRate>
//    <GovLength>25</GovLength>
//    <H265Profile>Main</H265Profile>
//    <SVC>
//      <enabled>false</enabled>
//    </SVC>
//    <smoothing>50</smoothing>
//    <SmartCodec>
//      <enabled>true</enabled>
//    </SmartCodec>
//  </Video>
//</StreamingChannel>";
//    }

//    // @"<Video xmlns="">
//    //     <enabled>true</enabled>
//    //     <videoInputChannelID>1</videoInputChannelID>
//    //     <videoCodecType>H.265</videoCodecType>
//    //     <videoResolutionWidth>1920</videoResolutionWidth>
//    //     <videoScanType>progressive</videoScanType>
//    //     <videoResolutionHeight>1080</videoResolutionHeight>
//    //     <videoQualityControlType>cbr</videoQualityControlType>
//    //     <constantBitRate>4096</constantBitRate>
//    //     <maxFrameRate>2500</maxFrameRate>
//    //     <GovLength>25</GovLength>
//    //     <H265Profile>Main</H265Profile>
//    //     <SVC>
//    //         <enabled>false</enabled>
//    //     </SVC>
//    //     <smoothing>50</smoothing>
//    //     <SmartCodec>
//    //         <enabled>true</enabled>
//    //     </SmartCodec>
//    //     <vbrAverageCap>2048</vbrAverageCap>


//    // </Video>"
//}


