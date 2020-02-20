namespace LametricApp

module ServiceHelper =
    open Microsoft.Extensions.Configuration
    open Microsoft.Extensions.Logging
    open Microsoft.AspNetCore.Http
    open FSharp.Control.Tasks.V2.ContextInsensitive
    open Giraffe
    open Chessie.ErrorHandling
    
    let fetchConfigurationEntry (config: IConfiguration) (name: string) =
        FSharpx.FSharpOption.ToFSharpOption(config.GetValue<string>(name))
        |> Trial.failIfNone (sprintf "Failed reading config entry: %s" name)

    let sendCustomWebResponse (logger: ILogger) (next : HttpFunc) (ctx : HttpContext) (contentType: string)
                        (result: Result<string, string>) =
        match result with
        | Ok (json, _) -> logger.LogDebug("Json {0}", json)
                          ctx.SetContentType(contentType)
                          ctx.WriteStringAsync(json)
        | Bad msgs     -> task {
                             logger.LogError("kengaroosHandler: {0}", String.concat "\n" msgs)
                             return! ServerErrors.INTERNAL_ERROR "Internal error" next ctx
                          }
        
    let sendWebResponse (logger: ILogger) (next : HttpFunc) (ctx : HttpContext) (result: Result<string, string>) =
        sendCustomWebResponse logger next ctx "application/json" result

