namespace LaMetric.Eklase

module EklaseWebClient =
    open System.Net
    open System.Net.Http
    open System.Net.Http.Json
    open System.Threading

    type EklaseMessageHandler() =
        inherit HttpClientHandler()

        member this.AddHeaders(message: HttpRequestMessage) =
            message.Headers.Add(
                "User-Agent",
                "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.81 Safari/537.36"
            )

        override this.Send(message: HttpRequestMessage, token: CancellationToken) =
            this.AddHeaders(message)
            ``base``.Send(message, token)

        override this.SendAsync(message: HttpRequestMessage, token: CancellationToken) =
            this.AddHeaders(message)
            ``base``.SendAsync(message, token)

    let clientHandler = new EklaseMessageHandler()
    clientHandler.CookieContainer <- new CookieContainer()

    let httpClient = new HttpClient(clientHandler)

    let login (url: string) username password =
        httpClient
            .PostAsJsonAsync(
                url,
                {| uname_f = ""
                   upass_f = ""
                   UserName = username
                   Password = password |}
            )
            .Wait()

    let fetchPage (url: string) = httpClient.GetStringAsync(url).Result
