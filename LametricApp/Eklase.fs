namespace LametricApp

module Eklase = 

    open Microsoft.AspNetCore.Http
    open Microsoft.Extensions.Logging
    open Microsoft.Extensions.Configuration
    open Giraffe
    open LaMetric.Eklase
    open Chessie.ErrorHandling

    type EklaseConfig = {
        diaryUrl: string
        loginUrl: string
        markUrl: string
        logoutUrl: string
        username: string
        password: string
    }

    let private getEklaseConfig (ctx: HttpContext) =
        trial {
            let config = ctx.GetService<IConfiguration>()

            let! diaryUrl = ServiceHelper.fetchConfigurationEntry config "EKLASE_DIARY_URL"
            let! loginUrl = ServiceHelper.fetchConfigurationEntry config "EKLASE_LOGIN_URL"
            let! markUrl = ServiceHelper.fetchConfigurationEntry config "EKLASE_MARK_URL"
            let! logoutUrl = ServiceHelper.fetchConfigurationEntry config "EKLASE_LOGOUT_URL"
            let! username = ServiceHelper.fetchConfigurationEntry config "EKLASE_USERNAME"
            let! password = ServiceHelper.fetchConfigurationEntry config "EKLASE_PASSWORD"
        
            return {
                diaryUrl = diaryUrl
                loginUrl = loginUrl
                markUrl = markUrl
                logoutUrl = logoutUrl
                username = username
                password = password
            }
        }

    let private fetchJson (ctx: HttpContext) (logger: ILogger) =
        trial {
            let! config = getEklaseConfig ctx

            let today = NotificationGenerator.getToday ()
            let fullDiaryUrl = NotificationGenerator.getFullDiaryUrl config.diaryUrl today
            let! lessonDays = NotificationGenerator.fetchParsedDiary
                                config.loginUrl
                                fullDiaryUrl
                                config.markUrl
                                config.logoutUrl
                                config.username
                                config.password

            logger.LogDebug("Fetched information for {0} days", lessonDays.Length)

            return NotificationGenerator.generateSerializedFrames lessonDays today
        }
        

    let eklaseHandler : HttpHandler = 
        fun (next : HttpFunc) (ctx : HttpContext) ->
            let logger = ctx.GetLogger("eklaseHandler")
            logger.LogInformation("eklaseHandler started")

            let result = fetchJson ctx logger
            ServiceHelper.sendWebResponse logger next ctx result

