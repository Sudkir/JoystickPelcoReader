using System.Net;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace ModulePelcoD.Hikvision
{
    /// <summary>
    /// Класс для отправки PTZ-команд на IP-камеры Hikvision через HTTP API.
    /// Поддерживает управление положением камеры, пресетами и получение информации об устройстве.
    /// </summary>
    public class PtzHttpSender
    {
        /// <summary>
        /// Экземпляр HTTP-клиента, авторизованного для работы с камерой.
        /// </summary>
        public HttpClient PtzHttpClient { get; set; }

        /// <summary>
        /// Номер канала камеры (по умолчанию "1").
        /// </summary>
        public string Channel { get; set; }

        /// <summary>
        /// Создаёт новый объект для управления PTZ-камерой.
        /// </summary>
        /// <param name="ip">IP-адрес камеры.</param>
        /// <param name="userName">Имя пользователя для авторизации.</param>
        /// <param name="password">Пароль пользователя.</param>
        /// <param name="channel">Номер канала камеры (по умолчанию 1).</param>
        public PtzHttpSender(string ip, string userName, string password, string channel = "1", int timeoutMs = 0_500)
        {
            try
            {
                Channel = channel;
                PtzHttpClient = PtzHttpClientFactory.GetClient(ip, userName, password, timeoutMs);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Ошибка инициализации Hikvision PtzHttpSender", ex);
            }
        }

        /// <summary>
        /// Формирует XML-контент для HTTP-запроса.
        /// </summary>
        /// <param name="bodyXml">Тело XML без заголовка.</param>
        /// <returns>Объект <see cref="StringContent"/> с корректным заголовком.</returns>
        public StringContent StringContentBuilder(string bodyXml)
        {
            var xmlContent = @$"<?xml version=""1.0"" encoding=""UTF-8""?>{bodyXml}";
            return new StringContent(xmlContent, Encoding.UTF8, "application/xml");
        }

        /// <summary>
        /// Обрабатывает ответ от камеры, выводит диагностическую информацию при ошибках.
        /// </summary>
        /// <param name="response">HTTP-ответ от камеры.</param>
        public async Task ResponseStatus(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                Uri? requestedUri = response.RequestMessage?.RequestUri;

                var xml = await response.Content.ReadAsStringAsync();

                if (response.StatusCode is HttpStatusCode.Forbidden)
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(xml);

                    var xmlInnerText = xmlDoc.InnerText;
                    Console.WriteLine($"Failed Uri:[{requestedUri}] -> HttpResponseMessage\nStatusCode:[{response.StatusCode}]\nXML response:{xmlInnerText}");
                }
                else
                {
                    Console.WriteLine($"Failed Uri:[{requestedUri}] -> HttpResponseMessage StatusCode:[{response.StatusCode}]");
                }
            }
        }

        /// <summary>
        /// Устанавливает скорость движения камеры для предустановленных положений.
        /// </summary>
        /// <param name="presetSpeed">Скорость (от 1 до 8).</param>
        /// <returns>HTTP-ответ от камеры.</returns>
        public async Task<HttpResponseMessage> SetPresetSpeed(int presetSpeed)
        {
            if (string.IsNullOrEmpty(Channel)) throw new NullReferenceException(nameof(Channel));

            try
            {
                var uri = string.Format("/ISAPI/PTZCtrl/channels/{0}", Channel);
                HttpResponseMessage getResponse = await PtzHttpClient.GetAsync(uri);
                await ResponseStatus(getResponse);

                var currentXml = await getResponse.Content.ReadAsStringAsync();
                var xmlDoc = XDocument.Parse(currentXml);
                var ns = XNamespace.Get("http://www.hikvision.com/ver20/XMLSchema");

                // Находим и обновляем presetSpeed
                var presetSpeedElement = xmlDoc.Descendants(ns + "presetSpeed").FirstOrDefault();
                if (presetSpeedElement != null)
                {
                    presetSpeedElement.Value = presetSpeed.ToString();
                }
                else
                {
                    var ptzChannel = xmlDoc.Descendants(ns + "PTZChannel").First();
                    ptzChannel.Add(new XElement(ns + "presetSpeed", presetSpeed));
                }

                var requestContent = new StringContent(xmlDoc.ToString(), Encoding.UTF8, "text/xml");

                // Отправляем PUT запрос для изменения скорости
                HttpResponseMessage putResponse = await PtzHttpClient.PutAsync(uri, requestContent);
                await ResponseStatus(putResponse);

                return putResponse;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Request error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
        }

        /// <summary>
        /// Перемещает камеру в заданную позицию (панорама, наклон, зум).
        /// </summary>
        /// <param name="xPan">Горизонтальный угол (пан).</param>
        /// <param name="yTilt">Вертикальный угол (тилт).</param>
        /// <param name="zZoom">Увеличение (зум), по умолчанию 0.</param>
        /// <returns>HTTP-ответ от камеры.</returns>
        public async Task<HttpResponseMessage> SetPosition(float xPan, float yTilt, float zZoom = 0)
        {
            if (string.IsNullOrEmpty(Channel)) throw new NullReferenceException(Channel);
            try
            {
                var content = StringContentBuilder($@"<PTZData><pan>{xPan}</pan><tilt>{yTilt}</tilt><zoom>{zZoom}</zoom></PTZData>");
                var uri = string.Format("/ISAPI/PTZCtrl/channels/{0}/continuous", Channel);
                HttpResponseMessage putResponse = await PtzHttpClient.PutAsync(uri, content);
                await ResponseStatus(putResponse);
                return putResponse;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Request error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            return new HttpResponseMessage();
        }

        /// <summary>
        /// Создаёт или обновляет предустановку (preset) с указанным индексом.
        /// </summary>
        /// <param name="presetIndex">Номер предустановки.</param>
        /// <returns>HTTP-ответ от камеры.</returns>
        public async Task<HttpResponseMessage> SetPreset(int presetIndex)
        {
            if (string.IsNullOrEmpty(Channel)) throw new NullReferenceException(Channel);
            try
            {
                var content = StringContentBuilder($@"<PTZPreset><enabled>true</enabled><id>{presetIndex}</id><presetName>preset_{presetIndex}</presetName></PTZPreset>");
                var uri = $"/ISAPI/PTZCtrl/channels/{Channel}/presets/{presetIndex}";
                HttpResponseMessage putResponse = await PtzHttpClient.PutAsync(uri, content);
                await ResponseStatus(putResponse);
                return putResponse;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Request error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            return new HttpResponseMessage();
        }

        /// <summary>
        /// Перемещает камеру в сохранённое положение (preset).
        /// </summary>
        /// <param name="presetIndex">Номер предустановки.</param>
        /// <returns>HTTP-ответ от камеры.</returns>
        public async Task<HttpResponseMessage> CallPreset(int presetIndex)
        {
            if (string.IsNullOrEmpty(Channel)) throw new NullReferenceException(Channel);
            try
            {
                var uri = string.Format("/ISAPI/PTZCtrl/channels/{0}/presets/{1}/goto", Channel, presetIndex);
                var content = StringContentBuilder(string.Empty);
                HttpResponseMessage putResponse = await PtzHttpClient.PutAsync(uri, content);
                await ResponseStatus(putResponse);
                return putResponse;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Request error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            return new HttpResponseMessage();
        }

        /// <summary>
        /// Получает общую информацию об устройстве (модель, серийный номер и т.п.).
        /// </summary>
        /// <returns>HTTP-ответ от камеры.</returns>
        public async Task<HttpResponseMessage> GetCameraInfo()
        {
            if (string.IsNullOrEmpty(Channel)) throw new NullReferenceException(Channel);
            try
            {
                var uri = string.Format("/ISAPI/System/deviceInfo");
                HttpResponseMessage getResponse = await PtzHttpClient.GetAsync(uri);
                await ResponseStatus(getResponse);
                return getResponse;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Request error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            return new HttpResponseMessage();
        }

        /// <summary>
        /// Получает конфигурацию PTZ-контроллера камеры (каналы, поддерживаемые параметры).
        /// </summary>
        /// <returns>HTTP-ответ от камеры.</returns>
        public async Task<HttpResponseMessage> GetCameraPTZCtrl()
        {
            if (PtzHttpClient == null) throw new NullReferenceException(nameof(PtzHttpClient));

            try
            {
                var uri = string.Format("/ISAPI/PTZCtrl/channels");
                HttpResponseMessage getResponse = await PtzHttpClient.GetAsync(uri);
                await ResponseStatus(getResponse);
                return getResponse;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Request error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            return new HttpResponseMessage();
        }
    }
}