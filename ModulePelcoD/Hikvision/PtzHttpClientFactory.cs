using System.Collections.Concurrent;
using System.Net;

namespace ModulePelcoD.Hikvision
{
    /// <summary>
    /// Фабрика для создания и повторного использования HTTP-клиентов,
    /// настроенных для работы с PTZ-камерами Hikvision.
    /// Реализует кэширование клиентов по ключу <c>authority</c>.
    /// </summary>
    public static class PtzHttpClientFactory
    {
        /// <summary>
        /// Кэш созданных клиентов, где ключ — это <c>authority</c> (host + порт).
        /// </summary>
        private static readonly ConcurrentDictionary<string, HttpClient> _clients = new();

        /// <summary>
        /// Получает или создаёт новый <see cref="HttpClient"/>, 
        /// предварительно настроенный для авторизации Digest и работы с камерой.
        /// </summary>
        /// <param name="ip">IP-адрес камеры.</param>
        /// <param name="user">Имя пользователя для аутентификации.</param>
        /// <param name="password">Пароль пользователя.</param>
        /// <param name="timeoutMs">Таймаут HTTP-запросов в миллисекундах (по умолчанию 1000).</param>
        /// <returns>Экземпляр <see cref="HttpClient"/>, готовый к работе с API камеры.</returns>
        public static HttpClient GetClient(
            string ip,
            string user,
            string password,
            int timeoutMs = 1_000)
        {
            var baseUri = new Uri($"http://{ip}/");
            var authority = baseUri.GetLeftPart(UriPartial.Authority);

            return _clients.GetOrAdd(authority, _ =>
            {
                var handler = new SocketsHttpHandler
                {
                    // Обновление DNS, чтобы не залипать на старом IP
                    PooledConnectionLifetime = TimeSpan.FromMinutes(2),
                    // Digest: сначала challenge → затем ответ; PreAuthenticate оставляем false
                    Credentials = BuildDigestCredentials(authority, user, password),
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                };

                var client = new HttpClient(handler, disposeHandler: true)
                {
                    BaseAddress = baseUri,
                    Timeout = TimeSpan.FromMilliseconds(timeoutMs)
                };

                client.DefaultRequestHeaders.ExpectContinue = false;
                client.DefaultRequestHeaders.Add("Accept", "application/json, */*");
                return client;
            });
        }

        /// <summary>
        /// Формирует кэш учётных данных для Digest-аутентификации.
        /// </summary>
        /// <param name="authority">URI камеры (host + порт).</param>
        /// <param name="user">Имя пользователя.</param>
        /// <param name="password">Пароль пользователя.</param>
        /// <returns>
        /// Объект <see cref="CredentialCache"/>, содержащий настройки Digest-аутентификации.
        /// </returns>
        private static CredentialCache BuildDigestCredentials(string authority, string user, string password)
        {
            var cache = new CredentialCache
            {
                { new Uri(authority), "Digest", new NetworkCredential(user, password) }
            };
            return cache;
        }
    }
}