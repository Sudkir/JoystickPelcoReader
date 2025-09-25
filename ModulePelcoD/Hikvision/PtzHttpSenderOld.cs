//using Microsoft.Extensions.Configuration;
//using System.Net;
//using System.Numerics;
//using System.Text;
//using System.Xml;
//using System.Xml.Linq;

//namespace ModulePelcoD.Hikvision
//{
//    public class PtzHttpSender
//    {
//        //<?xml version: "1.0" encoding="UTF-8"?><PTZData><zoom>100</zoom></PTZData>

//        public string CameraIP { get; set; }

//        public string CameraChannel { get; set; }
//        public string UserName { get; set; }
//        public string Password { get; set; }
//        public int TimeoutMs { get; set; } = 500;

//        public readonly string InfoUri = "/ISAPI/System/deviceInfo"; //http://172.168.10.101/ISAPI/System/deviceInfo
//        public readonly string PTZCtrlUri = "/ISAPI/PTZCtrl/channels";

//        public PtzHttpSender(string ip, string userName, string password, string channel = "1")
//        {
//           // HttpClient client = PtzHttpClientFactory.GetClient(GetUri(PTZCtrlUri), UserName, Password, TimeoutMs);

//            CameraIP = ip;
//            UserName = userName;
//            Password = password;
//            CameraChannel = channel;

//            IConfiguration config = new ConfigurationBuilder()
//                .AddJsonFile("appsettings.json")
//                .Build();
//            IConfigurationSection section = config.GetSection("Settings");
//            TimeoutMs = Convert.ToInt32(section.GetSection("TimeoutMs").Value);
//        }

//        public PtzHttpSender(string ip, string channel = "1")
//        {
//            CameraIP = ip;
//            CameraChannel = channel;

//            IConfiguration config = new ConfigurationBuilder()
//                .AddJsonFile("appsettings.json")
//                .Build();
//            IConfigurationSection section = config.GetSection("Settings");
//            TimeoutMs = Convert.ToInt32(section.GetSection("TimeoutMs").Value);
//        }

//        public PtzHttpSender()
//        {
//            IConfiguration config = new ConfigurationBuilder()
//                .AddJsonFile("appsettings.json")
//                .Build();
//            IConfigurationSection section = config.GetSection("Settings");
//            TimeoutMs = Convert.ToInt32(section.GetSection("TimeoutMs").Value);
//        }

//        public CredentialCache GetCredentialCache(Uri uri)
//        {
//            return new CredentialCache
//            {
//                {
//                    new Uri(uri.GetLeftPart(UriPartial.Authority)), // request url's host
//                    "Digest",  // authentication type
//                    new NetworkCredential(UserName, Password) // credentials
//                }
//            };
//        }

//        /// <summary>
//        ///
//        /// </summary>
//        /// <returns></returns>
//        private float GetAcceleration()
//        {
//            return 1.0F;
//        }

//        #region Info

//        private string GetUri(string str)
//        {
//            return $"http://{CameraIP}{str}";
//        }

//        public async Task<HttpResponseMessage> GetCameraPTZCtrl()
//        {
//            /*
//         *
//           <panSupport>true</panSupport>
//           <tiltSupport>true</tiltSupport>
//           <zoomSupport>true</zoomSupport>
//         *
//         */

//            if (string.IsNullOrEmpty(CameraIP)) throw new NullReferenceException(CameraIP);
//            if (string.IsNullOrEmpty(UserName)) throw new NullReferenceException(UserName);
//            try
//            {
//                using var handler = new HttpClientHandler();
//                handler.Credentials = GetCredentialCache(new Uri(GetUri(PTZCtrlUri)));
//                using var client = new HttpClient(handler);
//                client.Timeout = TimeSpan.FromMilliseconds(TimeoutMs);

//                HttpResponseMessage response = await client.GetAsync(GetUri(PTZCtrlUri));
//                Console.WriteLine($"Status Code: {response.StatusCode}");
//                Console.WriteLine($"Response: {await response.Content.ReadAsStringAsync()}");
//                return response;
//            }
//            catch (HttpRequestException ex)
//            {
//                Console.WriteLine($"Request error: {ex.Message}");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error: {ex.Message}");
//            }
//            return new HttpResponseMessage();
//        }

//        /// <summary>
//        ///
//        /// </summary>
//        /// <returns></returns>
//        /// <exception cref="NullReferenceException"></exception>
//        public async Task<HttpResponseMessage> GetCameraInfo()
//        {
//            if (string.IsNullOrEmpty(CameraIP)) throw new NullReferenceException(CameraIP);
//            if (string.IsNullOrEmpty(UserName)) throw new NullReferenceException(UserName);
//            try
//            {
//                using var handler = new HttpClientHandler();
//                handler.Credentials = GetCredentialCache(new Uri(GetUri(InfoUri)));
//                using var client = new HttpClient(handler);
//                client.Timeout = TimeSpan.FromMilliseconds(TimeoutMs);

//                HttpResponseMessage response = await client.GetAsync(GetUri(InfoUri));
//                Console.WriteLine($"Status Code: {response.StatusCode}");
//                Console.WriteLine($"Response: {await response.Content.ReadAsStringAsync()}");
//                return response;
//            }
//            catch (HttpRequestException ex)
//            {
//                Console.WriteLine($"Request error: {ex.Message}");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error: {ex.Message}");
//            }
//            return new HttpResponseMessage();
//        }

//        #endregion Info

//        #region Position

//        /// <summary>
//        ///
//        /// </summary>
//        /// <returns>http://172.168.10.101/ISAPI/PTZCtrl/channels/1/continuous</returns>
//        public string GetPositionUri()
//        {
//            return $"http://{CameraIP}/ISAPI/PTZCtrl/channels/{CameraChannel}/continuous";
//        }

//        /// <summary>
//        ///
//        /// </summary>
//        /// <param name="xPan">The x component corresponds to pan</param>
//        /// <param name="yTilt">The y component corresponds to tilt</param>
//        /// <param name="zZoom"></param>
//        /// <returns></returns>
//        private StringContent GetXmlContentString(float xPan, float yTilt, float zZoom = 0)
//        {
//            var acceleration = GetAcceleration();

//            float _xPan = xPan * acceleration; //pan X
//            float _yTilt = yTilt * acceleration; //tilt Y
//            float _zZoom = zZoom * acceleration; //zoom Z

//            var xmlContent = @$"<?xml version=""1.0"" encoding=""UTF-8""?><PTZData><pan>{_xPan}</pan><tilt>{_yTilt}</tilt><zoom>{_zZoom}</zoom></PTZData>";
//            var content = new StringContent(xmlContent, Encoding.UTF8, "application/xml");

//            return content;
//        }

//        /// <summary>
//        /// PUT /ISAPI/PTZCtrl/channels/1/presets/1
//        /// </summary>
//        /// <param name="presetIndex"></param>
//        /// <returns></returns>
//        /// <exception cref="NullReferenceException"></exception>
//        public async Task<HttpResponseMessage> SetPreset(int presetIndex)
//        {
//            if (string.IsNullOrEmpty(CameraIP)) throw new NullReferenceException(CameraIP);
//            if (string.IsNullOrEmpty(UserName)) throw new NullReferenceException(UserName);

//            var content = GetPresetsXmlContentString(presetIndex);
//            try
//            {
//                using var handler = new HttpClientHandler();
//                handler.Credentials = GetCredentialCache(new Uri(SetPresetUri(presetIndex)));
//                using var client = new HttpClient(handler);
//                client.Timeout = TimeSpan.FromMilliseconds(TimeoutMs);

//                HttpResponseMessage response = await client.PutAsync(SetPresetUri(presetIndex), content);
//                Console.WriteLine($"Status Code: {response.StatusCode}");
//                Console.WriteLine($"Response: {await response.Content.ReadAsStringAsync()}");

//                var xml = await response.Content.ReadAsStringAsync();

//                if (response.StatusCode is HttpStatusCode.Forbidden)
//                {
//                    XmlDocument xmlDoc = new XmlDocument();
//                    xmlDoc.LoadXml(xml);

//                    var xmlInnerText = xmlDoc.InnerText;
//                    throw new Exception(xmlInnerText);
//                }
//                return response;
//            }
//            catch (HttpRequestException ex)
//            {
//                Console.WriteLine($"Request error: {ex.Message}");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error: {ex.Message}");
//            }
//            return new HttpResponseMessage();
//        }

//        private string SetPresetUri(int presetIndex)
//        {
//            return $"http://{CameraIP}/ISAPI/PTZCtrl/channels/{CameraChannel}/presets/{presetIndex}";
//        }

//        private string CallPresetUri(int presetIndex, int speed = 90)
//        {
//            return $"http://{CameraIP}/ISAPI/PTZCtrl/channels/{CameraChannel}/presets/{presetIndex}/goto";
//            // return $"http://{CameraIP}/ISAPI/PTZCtrl/channels/{CameraChannel}/presets/{presetIndex}/goto?speed={speed}";
//        }

//        /// <summary>
//        /// PUT /ISAPI/PTZCtrl/channels/1 - Изменение скорости предустановки
//        /// </summary>
//        /// <param name="presetSpeed">Скорость предустановки (обычно 1-8)</param>
//        /// <returns></returns>
//        /// <exception cref="NullReferenceException"></exception>
//        public async Task<HttpResponseMessage> SetPresetSpeed(int presetSpeed)
//        {
//            if (string.IsNullOrEmpty(CameraIP)) throw new NullReferenceException(nameof(CameraIP));
//            if (string.IsNullOrEmpty(UserName)) throw new NullReferenceException(nameof(UserName));

//            try
//            {
//                using var handler = new HttpClientHandler();
//                var uri = new Uri(SetPresetSpeedUri());
//                handler.Credentials = GetCredentialCache(uri);
//                using var client = new HttpClient(handler);
//                client.Timeout = TimeSpan.FromMilliseconds(TimeoutMs);

//                // Сначала получаем текущие настройки канала
//                var getResponse = await client.GetAsync(uri);

//                if (!getResponse.IsSuccessStatusCode)
//                {
//                    Console.WriteLine($"Failed to get current PTZ settings: {getResponse.StatusCode}");
//                    return getResponse;
//                }

//                var currentXml = await getResponse.Content.ReadAsStringAsync();
//                Console.WriteLine($"Current settings XML: {currentXml}");

//                // Парсим текущие настройки и обновляем только presetSpeed
//                var xmlDoc = XDocument.Parse(currentXml);
//                var ns = XNamespace.Get("http://www.hikvision.com/ver20/XMLSchema");

//                // Находим и обновляем presetSpeed
//                var presetSpeedElement = xmlDoc.Descendants(ns + "presetSpeed").FirstOrDefault();
//                if (presetSpeedElement != null)
//                {
//                    presetSpeedElement.Value = presetSpeed.ToString();
//                }
//                else
//                {
//                    // Если элемента нет, добавляем его
//                    var ptzChannel = xmlDoc.Descendants(ns + "PTZChannel").First();
//                    ptzChannel.Add(new XElement(ns + "presetSpeed", presetSpeed));
//                }

//                var updatedXml = xmlDoc.ToString();
//                Console.WriteLine($"Updated XML: {updatedXml}");

//                var requestContent = new StringContent(updatedXml, Encoding.UTF8, "text/xml");

//                // Отправляем PUT запрос для изменения скорости
//                HttpResponseMessage response = await client.PutAsync(uri, requestContent);

//                Console.WriteLine($"Status Code: {response.StatusCode}");
//                Console.WriteLine($"Response: {await response.Content.ReadAsStringAsync()}");

//                return response;
//            }
//            catch (HttpRequestException ex)
//            {
//                Console.WriteLine($"Request error: {ex.Message}");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error: {ex.Message}");
//            }

//            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
//        }

//        /// <summary>
//        /// Формирует URI для изменения настроек PTZ канала
//        /// </summary>
//        /// <returns></returns>
//        private string SetPresetSpeedUri()
//        {
//            return $"http://{CameraIP}/ISAPI/PTZCtrl/channels/1";
//        }



//        /// <summary>
//        /// PUT /ISAPI/PTZCtrl/channels/1/presets/1/goto
//        /// </summary>
//        /// <param name="presetIndex"></param>
//        /// <returns></returns>
//        /// <exception cref="NullReferenceException"></exception>
//        public async Task<HttpResponseMessage> CallPreset(int presetIndex)
//        {
//            if (string.IsNullOrEmpty(CameraIP)) throw new NullReferenceException(CameraIP);
//            if (string.IsNullOrEmpty(UserName)) throw new NullReferenceException(UserName);

//            try
//            {
//                using var handler = new HttpClientHandler();
//                handler.Credentials = GetCredentialCache(new Uri(CallPresetUri(presetIndex)));
//                using var client = new HttpClient(handler);
//                client.Timeout = TimeSpan.FromMilliseconds(TimeoutMs);

//                //HttpResponseMessage response = await client.PutAsync(new Uri(CallPresetUri(presetIndex)));

//                // Создаем PUT запрос с пустым содержимым, как требует Hikvision API
//                var requestUri = new Uri(CallPresetUri(presetIndex));
//                var xmlContent = @$"<?xml version=""1.0"" encoding=""UTF-8""?><PTZData><presetSpeed>9</presetSpeed></PTZData>";
//                var requestContent = new StringContent(xmlContent, Encoding.UTF8, "application/xml");

//                HttpResponseMessage response = await client.PutAsync(requestUri, requestContent);
//                Console.WriteLine($"Status Code: {response.StatusCode}");
//                Console.WriteLine($"Response: {await response.Content.ReadAsStringAsync()}");
//                return response;
//            }
//            catch (HttpRequestException ex)
//            {
//                Console.WriteLine($"Request error: {ex.Message}");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error: {ex.Message}");
//            }
//            return new HttpResponseMessage();
//        }

//        private StringContent GetPresetsXmlContentString(int presetIndex)
//        {
//            //< requestURL >/ ISAPI / PTZCtrl / channels / 1 / presets / 1 /goto</ requestURL >

//            //PUT /ISAPI/PTZCtrl/channels/1/presets/1
//            //< enabled > true </ enabled >
//            //< id > 1 </ id >
//            //< presetName > NetSpeedDome_preset 1 </ presetName >
//            //</ PTZPreset >

//            var xmlContent = @$"<?xml version=""1.0"" encoding=""UTF-8""?><PTZPreset><enabled>true</enabled><id>{presetIndex}</id><presetName>preset_{presetIndex}</presetName></PTZPreset>";
//            var content = new StringContent(xmlContent, Encoding.UTF8, "application/xml");
//            return content;
//        }

//        /// <summary>
//        /// SetPosition(X, Y, ZOOM)
//        /// </summary>
//        /// <param name="xPan"></param>
//        /// <param name="yTilt"></param>
//        /// <param name="zZoom"></param>
//        /// <returns></returns>
//        public async Task<HttpResponseMessage> SetPosition(float xPan, float yTilt, float zZoom = 0)
//        {
//            // http://172.168.10.55/ISAPI/PTZCtrl/channels/1/continuous
//            //"1.0" encoding="UTF-8"?><PTZData><pan>60</pan><tilt>0</tilt></PTZData>

//            if (string.IsNullOrEmpty(CameraIP)) throw new NullReferenceException(CameraIP);
//            if (string.IsNullOrEmpty(UserName)) throw new NullReferenceException(UserName);

//            var content = GetXmlContentString(xPan, yTilt, zZoom);

//            try
//            {
//                using var handler = new HttpClientHandler();
//                handler.Credentials = GetCredentialCache(new Uri(GetPositionUri()));
//                using var client = new HttpClient(handler);
//                client.Timeout = TimeSpan.FromMilliseconds(TimeoutMs);

//                HttpResponseMessage response = await client.PutAsync(GetPositionUri(), content);
//                Console.WriteLine($"Status Code: {response.StatusCode}");
//                Console.WriteLine($"Response: {await response.Content.ReadAsStringAsync()}");

//                var xml = await response.Content.ReadAsStringAsync();

//                if (response.StatusCode is HttpStatusCode.Forbidden)
//                {
//                    XmlDocument xmlDoc = new XmlDocument();
//                    xmlDoc.LoadXml(xml);

//                    var xmlInnerText = xmlDoc.InnerText;
//                    throw new Exception(xmlInnerText);
//                }
//                return response;
//            }
//            catch (HttpRequestException ex)
//            {
//                Console.WriteLine($"Request error: {ex.Message}");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Error: {ex.Message}");
//            }
//            return new HttpResponseMessage();
//        }

//        /// <summary>
//        /// SetPosition(Vector3)
//        /// </summary>
//        /// <param name="vector3"></param>
//        /// <returns></returns>
//        public async Task<HttpResponseMessage> SetPosition(Vector3 vector3)
//        {
//            return await SetPosition(vector3.X, vector3.Y, vector3.Z);
//        }

//        #endregion Position
//    }
//}