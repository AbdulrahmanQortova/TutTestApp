namespace TutBackend.Services;
    /// <summary>
    /// Simple HTTP client wrapper for Qip API auth endpoints.
    /// </summary>
    public sealed class QipClient(HttpClient httpClient)
    {
        private readonly HttpClient _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        /// <summary>
        /// Convenience factory that creates an HttpClient with the given base address.
        /// </summary>
        public static QipClient Create(string baseAddress)
        {
            if (string.IsNullOrWhiteSpace(baseAddress))
                throw new ArgumentException("Base address must be provided", nameof(baseAddress));

            var hc = new HttpClient { BaseAddress = new Uri(baseAddress, UriKind.RelativeOrAbsolute) };
            return new QipClient(hc);
        }

        public async Task<HttpResponseMessage> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));
            return await _http.PostAsJsonAsync("/register", request, cancellationToken).ConfigureAwait(false);
        }

        public async Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));
            var resp = await _http.PostAsJsonAsync("/login", request, cancellationToken).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
            var token = await resp.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
            return token!;
        }

        public async Task<TokenResponse> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken = default)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));
            var resp = await _http.PostAsJsonAsync("/refresh", request, cancellationToken).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
            var token = await resp.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
            return token!;
        }

        public async Task<HttpResponseMessage> LogoutAsync(RefreshRequest request, CancellationToken cancellationToken = default)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));
            return await _http.PostAsJsonAsync("/logout", request, cancellationToken).ConfigureAwait(false);
        }

        public async Task<ValidateResponse> ValidateAsync(ValidateRequest request, CancellationToken cancellationToken = default)
        {
            // Shortcut validation for development
            if (string.IsNullOrEmpty(request.Token))
                return new ValidateResponse
                {
                    IsValid = false
                };
            string username = request.Token[7..];
            return new ValidateResponse
            {
                IsValid = true,
                Username = username
            };
            /*
            
            if (request is null) throw new ArgumentNullException(nameof(request));
            var resp = await _http.PostAsJsonAsync("/validate", request, cancellationToken).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
            var result = await resp.Content.ReadFromJsonAsync<ValidateResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
            return result!;
            */
        }
    }
