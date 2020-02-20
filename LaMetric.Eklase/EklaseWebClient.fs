namespace LaMetric.Eklase

module EklaseWebClient =

    open System
    open System.Net
    open System.Text
    open System.Collections.Specialized

    
    type EklaseWebClient() = 
        inherit WebClient()

        let cookieContainer = new CookieContainer()
        let mutable lastPage : string option = Option.None

        override this.GetWebRequest(address) =
            let r = base.GetWebRequest(address)
            let webRequest = r :?> HttpWebRequest
            webRequest.CookieContainer <- cookieContainer
            match lastPage with
            | Some p -> webRequest.Referer <- p
            | None -> ()

            lastPage <- address.ToString() |> Some

            webRequest.UserAgent <- "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.81 Safari/537.36"

            r


    let webClient = new EklaseWebClient()
    webClient.Encoding <- Encoding.UTF8

    let disableSslCertificateValidation () =
        ServicePointManager.ServerCertificateValidationCallback <- (fun _ _ _ _ -> true)

    let setProxy (proxyUrl: string) =
        webClient.Proxy <- (new WebProxy(proxyUrl) :> IWebProxy)

    let login (url: string) username password =
        let parameters = new NameValueCollection()
        parameters.Add("uname_f", "")
        parameters.Add("upass_f", "")
        parameters.Add("UserName", username)
        parameters.Add("Password", password)

        webClient.UploadValues(url, "POST", parameters)

    let fetchPage (url: string) =
        webClient.DownloadString(url)

