namespace LaMetric.Kengaroos.Tests

module ParserTests =
    open System

    open Xunit
    open Swensen.Unquote

    open LaMetric.Kengaroos
    open LaMetric.Kengaroos.DataTypes
    open LaMetric.Kengaroos.Tests.TestCommon

    [<Theory>]
    [<InlineData("Schedule1.html")>]
    [<InlineData("Schedule2.html")>]
    let ``parses Kengaroos calendar`` (htmlFileName : string) =
        let html = extractEmbeddedPage htmlFileName
        let result = CalendarDayParser.extractCalendarDaysFromHtml html

        test <@ List.isEmpty result |> not @>

