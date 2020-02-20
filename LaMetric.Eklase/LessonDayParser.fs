namespace LaMetric.Eklase

module LessonDayParser =

    open System
    open FSharp.Data
    open Chessie.ErrorHandling
    open NodaTime.Text
    open FSharpx.Text
    open FSharpx.Option

    open LaMetric.Common.TryParser
    open LaMetric.Eklase
    open LaMetric.Eklase.DataTypes

    type HomeWorkDetails = {
        Created: NodaTime.LocalDateTime;
        Author: string;
    }

    let extractLessonDays upgradeMark (node: HtmlNode) =
        let extractLessonDay (headingNode: HtmlNode) (lessonsTable: HtmlNode) =
            let extractLessonDate (heading: HtmlNode) =
                maybe {
                    let! m = heading.DirectInnerText().Trim()
                             |> Regex.tryMatch @"(\d{2}\.\d{2}\.\d{2})"
                    return! m.GroupValues.[0]
                            |> LocalDatePattern.CreateWithCurrentCulture("dd.MM.yy").Parse
                            |> Helpers.tryGetLocalDate
                }
                |> failIfNone "Failed to extract lesson date"

            let extractHomeWork (column: HtmlNode) =
                let extractHomeWorkDescription (homeWorkTag: HtmlNode) =
                    homeWorkTag.InnerText().Trim()
                    |> parseString

                let extractHomeWorkDetails (homeWorkTag: HtmlNode) =
                    maybe {
                        let! title = homeWorkTag.TryGetAttribute("title")
                        let! m = title.Value() |>
                                    Regex.tryMatch @"(\d{2}\.\d{2}.\d{4}\.\s+\d{2}:\d{2}):\s+([^(]+)"

                        let! created = m.GroupValues.[0]
                                        |> LocalDateTimePattern.CreateWithCurrentCulture("dd.MM.yyyy. HH:mm").Parse
                                        |> Helpers.tryGetLocalDate
                        let author = m.GroupValues.[1]

                        return {
                            Created = created;
                            Author = author;
                        }
                    }

                let findHomeWorkTag (column: HtmlNode) =
                    let findFirstTag selector =
                        column.CssSelect(selector) |> Seq.tryHead

                    ["span"; "p[title]"]
                    |> List.choose findFirstTag
                    |> List.tryHead

                maybe {
                    let! homeWorkTag = findHomeWorkTag column
                    let! homeWorkDescription = extractHomeWorkDescription homeWorkTag
                    let homeWorkDetails = extractHomeWorkDetails homeWorkTag
                    return {
                        Description = homeWorkDescription;
                        Author = Option.map (fun d -> d.Author) homeWorkDetails;
                        Created = Option.map (fun d -> d.Created) homeWorkDetails;
                    }
                }

            let extractColumn (row: HtmlNode) cssClass errorText =
                row.CssSelect("td." + cssClass)
                |> Seq.tryHead
                |> failIfNone errorText

            let extractHomeWorkInfo (lessonRowNode: HtmlNode) =
                let infoColumn = extractColumn lessonRowNode "info-content" "Failed to extract info column"
                match infoColumn with
                | Pass column -> extractHomeWork column
                | _ -> Option.None

            let extractLessonDetails (lessonRowNode: HtmlNode) =
                let extractLessonIndex (firstColumn: HtmlNode) =
                    firstColumn.CssSelect("span.number")
                    |> Seq.map (fun n -> n.InnerText().Trim().Trim('.'))
                    |> Seq.choose parseInt
                    |> Seq.tryHead
                    |> Option.defaultValue 0

                let extractLessonName (firstColumn: HtmlNode) =
                    firstColumn.CssSelect("span.title")
                    |> Seq.map (fun n -> n.DirectInnerText().Trim())
                    |> Seq.tryHead
                    |> failIfNone "Failed to extract lesson name"

                let extractLessonRoom (firstColumn: HtmlNode) =
                    firstColumn.CssSelect("span.room")
                    |> Seq.map (fun n -> n.InnerText().Trim())
                    |> Seq.tryHead

                let extractTheme (secondColumn: HtmlNode) =
                    secondColumn.InnerText().Trim()
                    |> parseString

                let extractScores (fourthColumn: HtmlNode) =
                    let extractScore (scoreTag: HtmlNode) =
                        let id = scoreTag.AttributeValue("data-id") |> parseInt64
                        let display = scoreTag.InnerText() |> parseString

                        match id, display with
                        | Some id, Some d -> Some { Id = id; Display = d }
                        | _ -> Option.None

                    fourthColumn.CssSelect("span.score[data-id]")
                    |> List.choose extractScore
                    |> List.map (fun s -> ScoreReference(s))
                    |> List.toArray

                trial {
                    let! firstColumn = extractColumn lessonRowNode "first-column" "Failed to extract first column"
                    let! secondColumn = extractColumn lessonRowNode "subject" "Failed to extract second column"
                    let! thirdColumn = extractColumn lessonRowNode "hometask" "Failed to extract thrid column"
                    let! fourthColumn = extractColumn lessonRowNode "score" "Failed to extract fourth column"
                    
                    let lessonIndex = extractLessonIndex firstColumn
                    let! lessonName = extractLessonName firstColumn
                    let lessonRoom = extractLessonRoom firstColumn
                    let theme = extractTheme secondColumn
                    let homeWork = extractHomeWork thirdColumn
                    let scores = extractScores fourthColumn |> Array.map upgradeMark

                    return Lesson {
                        Index = lessonIndex;
                        Name = lessonName;
                        RoomNumber = lessonRoom;
                        Theme = theme;
                        HomeWork = homeWork;
                        Scores = scores;
                    }
                }

            let isEmptyPeriod (lessonRowNode: HtmlNode) =
                lessonRowNode.CssSelect("td.no-data") |> List.isEmpty |> not

            let extractLessonEntry (lessonRowNode: HtmlNode) =
                if isEmptyPeriod lessonRowNode
                    then Option.None
                    else
                        let homeWorkInfo = extractHomeWorkInfo lessonRowNode
                        match homeWorkInfo with
                            | Some hw -> HomeWork hw |> ok |> Some
                            | Option.None -> extractLessonDetails lessonRowNode |> Some


            trial {
                let! lessonDate = extractLessonDate headingNode
                let! lessons = lessonsTable.CssSelect("tbody tr")
                               |> List.choose extractLessonEntry
                               |> collect
                return {
                    Date = lessonDate;
                    Lessons = List.toArray lessons
                }
            }

        let extractMainContainer (node: HtmlNode) =
            node.CssSelect(".student-journal-lessons-table-holder")
            |> Seq.tryHead
            |> failIfNone "Failed to extract main container"

        trial {
            let! mainContainer = extractMainContainer node
            let headings = mainContainer.CssSelect("h2")
            let lessons = mainContainer.CssSelect(".lessons-table")
            return! Seq.zip headings lessons
                    |> Seq.map (fun (headingNode, lessonsTable) -> extractLessonDay headingNode lessonsTable)
                    |> collect
        }

    let extractLessonDaysFromHtml upgradeMark html =
        let htmlDocument = HtmlDocument.Parse html
        extractLessonDays upgradeMark <| htmlDocument.Body()

