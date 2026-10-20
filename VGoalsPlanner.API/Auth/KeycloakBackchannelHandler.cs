namespace VGoalsPlanner.API.Auth
{
    /// <summary>
    /// Docker: API ходит в Keycloak не по тому адресу, что браузер. Токены и собственные URL Keycloak содержат
    /// "http://localhost:8083", а внутри контейнера localhost — это сам контейнер. Запросы этого API к публичному
    /// адресу Keycloak уходят на <c>Authentication:InternalAuthority</c> (например http://host.docker.internal:8083)
    /// с публичным заголовком Host, поэтому Keycloak отвечает так же, как браузеру (тот же издатель и URL).
    /// </summary>
    public sealed class KeycloakBackchannelHandler : DelegatingHandler
    {
        private readonly Uri _publicOrigin;
        private readonly Uri _internalOrigin;

        private KeycloakBackchannelHandler(Uri publicOrigin, Uri internalOrigin)
            : base(new SocketsHttpHandler())
        {
            _publicOrigin = publicOrigin;
            _internalOrigin = internalOrigin;
        }

        /// <summary>Обработчик, если задан <paramref name="internalAuthority"/> (Docker); null — ходить в Keycloak напрямую.</summary>
        public static HttpMessageHandler? Create(string? publicAuthority, string? internalAuthority)
        {
            if (string.IsNullOrWhiteSpace(internalAuthority) || string.IsNullOrWhiteSpace(publicAuthority))
            {
                return null;
            }

            return new KeycloakBackchannelHandler(new Uri(publicAuthority), new Uri(internalAuthority));
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri is { } uri
                && Uri.Compare(uri, _publicOrigin, UriComponents.SchemeAndServer, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) == 0)
            {
                request.Headers.Host = _publicOrigin.Authority;
                request.RequestUri = new UriBuilder(uri)
                {
                    Scheme = _internalOrigin.Scheme,
                    Host = _internalOrigin.Host,
                    Port = _internalOrigin.Port
                }.Uri;
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
