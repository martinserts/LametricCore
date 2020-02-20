namespace LaMetric.Eklase

module ScoreUpgrader =

    open System
    open FSharp.Data
    open Chessie.ErrorHandling

    open LaMetric.Eklase.DataTypes
    open LaMetric.Eklase
    open LaMetric.Eklase.EklaseWebClient
    
    
    let upgradeScoreReference markUrl scoreReference =
        let fullUrl = sprintf "%s?MarkId=%d" markUrl scoreReference.Id
        let html = fullUrl |> fetchPage
        MarkParser.extractMarkFromHtml html scoreReference

    let upgradeScore markUrl score =
        match score with
        | ScoreReference sr -> trial {
                                    let! scoreDetails = upgradeScoreReference markUrl sr
                                    return ScoreDetails scoreDetails
                               }
        | ScoreDetails _ -> score |> ok

