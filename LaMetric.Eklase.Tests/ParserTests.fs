namespace LaMetric.Eklase.Tests

module ParserTests =
    open System

    open Xunit
    open Swensen.Unquote
    open Chessie.ErrorHandling

    open LaMetric.Eklase
    open LaMetric.Eklase.DataTypes
    open LaMetric.Eklase.Tests.TestCommon

    [<Theory>]
    [<InlineData("Diary.html")>]
    [<InlineData("Diary2.html")>]
    [<InlineData("Diary3.html")>]
    [<InlineData("Diary4.html")>]
    let ``parses Eklase diary`` (htmlFileName : string) =
        let html = extractEmbeddedPage htmlFileName
        let result = LessonDayParser.extractLessonDaysFromHtml (fun s -> s) html

        test <@ failed result |> not @>


    [<Theory>]
    [<InlineData("Mark.html")>]
    [<InlineData("Mark2.html")>]
    let ``parses Eklase Score details`` (htmlFileName : string) =
        let html = extractEmbeddedPage htmlFileName

        let score = {
            Id = 1L;
            Display = "+"
        }

        let result = MarkParser.extractMarkFromHtml html score

        test <@ failed result |> not @>



