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
            if (response.IsSuccessStatusCode) return;

            try
            {
                Uri? requestedUri = response.RequestMessage?.RequestUri;
                var xml = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    try
                    {
                        var xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml(xml);
                        var xmlInnerText = xmlDoc.InnerText;
                        throw new InvalidOperationException(
                            $"Запрос не авторизован (403). Uri=[{requestedUri}] Ответ XML: {xmlInnerText}");
                    }
                    catch (XmlException)
                    {
                        // Если это не корректный XML — всё равно бросаем с оригинальным телом
                        throw new InvalidOperationException(
                            $"Запрос не авторизован (403). Uri=[{requestedUri}] Тело ответа: {xml}");
                    }
                }

                throw new InvalidOperationException(
                    $"HTTP ошибка {(int)response.StatusCode} ({response.StatusCode}). Uri=[{requestedUri}] Тело ответа: {xml}");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Ошибка обработки HTTP-ответа камеры {PtzHttpClient.BaseAddress}", ex);
            }
        }

        /// <summary>
        /// Устанавливает скорость движения камеры для предустановленных положений.
        /// </summary>
        /// <param name="presetSpeed">Скорость (от 1 до 8).</param>
        /// <returns>HTTP-ответ от камеры.</returns>
        public async Task<HttpResponseMessage> SetPresetSpeed(int presetSpeed)
        {
            if (string.IsNullOrWhiteSpace(Channel))
                throw new InvalidOperationException("Channel не установлен.");

            if (presetSpeed < 0) presetSpeed = 1;
            if (presetSpeed > 8) presetSpeed = 8;

            try
            {
                var uri = $"/ISAPI/PTZCtrl/channels/{Channel}";
                var getResponse = await PtzHttpClient.GetAsync(uri).ConfigureAwait(false);
                await ResponseStatus(getResponse).ConfigureAwait(false);

                var currentXml = await getResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                var xmlDoc = XDocument.Parse(currentXml);
                var ns = XNamespace.Get("http://www.hikvision.com/ver20/XMLSchema");

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

                var putResponse = await PtzHttpClient.PutAsync(uri, requestContent).ConfigureAwait(false);
                await ResponseStatus(putResponse).ConfigureAwait(false);

                return putResponse;
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException($"Ошибка HTTP при установке presetSpeed={presetSpeed} для {PtzHttpClient.BaseAddress}.", ex);
            }
            catch (XmlException ex)
            {
                throw new InvalidOperationException("Ошибка разбора XML-конфигурации PTZ канала.", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Не удалось установить presetSpeed={presetSpeed} для {PtzHttpClient.BaseAddress}.", ex);
            }
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
            if (string.IsNullOrWhiteSpace(Channel))
                throw new InvalidOperationException("Channel не установлен.");

            try
            {
                var content = StringContentBuilder($@"<PTZData><pan>{xPan}</pan><tilt>{yTilt}</tilt><zoom>{zZoom}</zoom></PTZData>");
                var uri = $"/ISAPI/PTZCtrl/channels/{Channel}/continuous";
                var putResponse = await PtzHttpClient.PutAsync(uri, content).ConfigureAwait(false);
                await ResponseStatus(putResponse).ConfigureAwait(false);
                return putResponse;
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException($"Ошибка HTTP при установке позиции PTZ для {PtzHttpClient.BaseAddress}.", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Не удалось установить позицию PTZ для {PtzHttpClient.BaseAddress}.", ex);
            }
        }

        /// <summary>
        /// Создаёт или обновляет предустановку (preset) с указанным индексом.
        /// </summary>
        /// <param name="presetIndex">Номер предустановки.</param>
        /// <returns>HTTP-ответ от камеры.</returns>
        public async Task<HttpResponseMessage> SetPreset(int presetIndex)
        {
            if (string.IsNullOrWhiteSpace(Channel))
                throw new InvalidOperationException("Channel не установлен.");

            try
            {
                var content = StringContentBuilder($@"<PTZPreset><enabled>true</enabled><id>{presetIndex}</id><presetName>preset_{presetIndex}</presetName></PTZPreset>");
                var uri = $"/ISAPI/PTZCtrl/channels/{Channel}/presets/{presetIndex}";
                var putResponse = await PtzHttpClient.PutAsync(uri, content).ConfigureAwait(false);
                await ResponseStatus(putResponse).ConfigureAwait(false);
                return putResponse;
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException($"Ошибка HTTP при создании/обновлении пресета {presetIndex} для {PtzHttpClient.BaseAddress}.", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Не удалось создать/обновить пресет {presetIndex} для {PtzHttpClient.BaseAddress}.", ex);
            }
        }

        /// <summary>
        /// Перемещает камеру в сохранённое положение (preset).
        /// </summary>
        /// <param name="presetIndex">Номер предустановки.</param>
        /// <returns>HTTP-ответ от камеры.</returns>
        public async Task<HttpResponseMessage> CallPreset(int presetIndex)
        {
            if (string.IsNullOrWhiteSpace(Channel))
                throw new InvalidOperationException("Channel не установлен.");

            try
            {
                var uri = $"/ISAPI/PTZCtrl/channels/{Channel}/presets/{presetIndex}/goto";
                var content = StringContentBuilder("<PTZData></PTZData>");
                var putResponse = await PtzHttpClient.PutAsync(uri, content).ConfigureAwait(false);
                await ResponseStatus(putResponse).ConfigureAwait(false);
                return putResponse;
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException($"Ошибка HTTP при переходе к пресету {presetIndex} для {PtzHttpClient.BaseAddress}.", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Не удалось перейти к пресету {presetIndex} для {PtzHttpClient.BaseAddress}.", ex);
            }
        }

        /// <summary>
        /// Получает общую информацию об устройстве (модель, серийный номер и т.п.).
        /// </summary>
        /// <returns>HTTP-ответ от камеры.</returns>
        public async Task<HttpResponseMessage> GetCameraInfo()
        {
            if (string.IsNullOrWhiteSpace(Channel))
                throw new InvalidOperationException("Channel не установлен.");

            try
            {
                var uri = "/ISAPI/System/deviceInfo";
                var getResponse = await PtzHttpClient.GetAsync(uri).ConfigureAwait(false);
                await ResponseStatus(getResponse).ConfigureAwait(false);
                return getResponse;
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException($"Ошибка HTTP при получении информации об {PtzHttpClient.BaseAddress}.", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Не удалось получить информацию об {PtzHttpClient.BaseAddress}.", ex);
            }
        }

        /// <summary>
        /// Получает конфигурацию PTZ-контроллера камеры (каналы, поддерживаемые параметры).
        /// </summary>
        /// <returns>HTTP-ответ от камеры.</returns>
        public async Task<HttpResponseMessage> GetCameraPTZCtrl()
        {
            if (PtzHttpClient == null)
                throw new InvalidOperationException("PtzHttpClient не инициализирован.");

            try
            {
                var uri = "/ISAPI/PTZCtrl/channels";
                var getResponse = await PtzHttpClient.GetAsync(uri).ConfigureAwait(false);
                await ResponseStatus(getResponse).ConfigureAwait(false);
                return getResponse;
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException($"Ошибка HTTP при получении конфигурации PTZ-контроллера для {PtzHttpClient.BaseAddress}.", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Не удалось получить конфигурацию PTZ-контроллера для {PtzHttpClient.BaseAddress}.", ex);
            }
        }
    }
}