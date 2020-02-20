namespace LametricApp

module Kengaroos = 

    open Microsoft.AspNetCore.Http
    open Microsoft.Extensions.Logging
    open Microsoft.Extensions.Configuration
    open Giraffe
    open LaMetric.Kengaroos
    open LaMetric.Kengaroos.IcalGenerator 
    open Chessie.ErrorHandling

    type KengaroosConfig = {
        calendarUrl: string
    }

    let private getKengaroosConfig (ctx: HttpContext) =
        trial {
            let config = ctx.GetService<IConfiguration>()

            let! calendarUrl = ServiceHelper.fetchConfigurationEntry config "KENGAROOS_CALENDAR_URL"
        
            return {
                calendarUrl = calendarUrl
            }
        }

    let private fetchJson (ctx: HttpContext) (logger: ILogger) =
        trial {
            let! config = getKengaroosConfig ctx
            let today = NotificationGenerator.getToday ()
            let calendarDays = NotificationGenerator.fetchParsedCalendar config.calendarUrl today

            logger.LogDebug("Fetched information for {0} days", calendarDays.Length)

            return NotificationGenerator.generateSerializedFrames calendarDays today
        }
        
    let private fetchIcal (ctx: HttpContext) (logger: ILogger) =
        trial {
            let! config = getKengaroosConfig ctx
            let today = NotificationGenerator.getToday ()
            let calendarDays = NotificationGenerator.fetchParsedCalendar config.calendarUrl today

            logger.LogDebug("Fetched information for {0} days", calendarDays.Length)

            return toIcal calendarDays
                   |> serializeCalendar
        }

    let kengaroosHandler : HttpHandler = 
        fun (next : HttpFunc) (ctx : HttpContext) ->
            let logger = ctx.GetLogger("kengaroosHandler")
            logger.LogInformation("kengaroosHandler started")

            let result = fetchJson ctx logger
            ServiceHelper.sendWebResponse logger next ctx result

            
    let kengaroosIcal : HttpHandler =
        fun (next : HttpFunc) (ctx : HttpContext) ->
            let logger = ctx.GetLogger("kengaroosIcal")
            logger.LogInformation("kengaroosIcal started")

            let result = fetchIcal ctx logger
            ServiceHelper.sendCustomWebResponse logger next ctx "text/calendar" result
