namespace LaMetric.Eklase

module MarkParser =

    open System

    open FSharp.Data
    open FSharpx.Text
    open FSharpx.Option
    open Chessie.ErrorHandling
    open NodaTime.Text

    open LaMetric.Common
    open LaMetric.Eklase.DataTypes

    type MarkDetailsEntry = {
        Topic: string;
        Value: string;
    }

    let extractMark (rootNode: HtmlNode) (scoreReference: ScoreReference) =
        let extractMarkText () =
            let parseMark mark =
                maybe {
                    let! m = Regex.tryMatch "Atzīme:\s*(\S+)" mark
                    return m.GroupValues.[0]
                }
               
            rootNode.CssSelect(".mark-card-inner-title span")
            |> List.map (fun n -> n.DirectInnerText().Trim())
            |> List.choose parseMark
            |> Seq.tryHead
            |> failIfNone "Failed to extract mark text" 

        let extractMarkDetails (paragraph: HtmlNode) =
            let extractDetailsTopic () =
                paragraph.CssSelect("span")
                |> List.map (fun n -> n.InnerText())
                |> Seq.tryHead
                |> failIfNone "Failed to extract mark details topic"

            let extractDetailsValue () =
                paragraph.Elements("")
                |> List.map (fun n -> n.InnerText().Trim())
                |> List.choose TryParser.parseString
                |> Seq.tryHead
                |> failIfNone "Failed to extract mark details value"

            trial {
                let! topic = extractDetailsTopic ()
                let! value = extractDetailsValue ()
                return {
                    Topic = topic;
                    Value = value;
                }
            }

        let extractAuthorAndCreated text =
            maybe {
                let! m = Regex.tryMatch @"([^\(]+)\((\d{2}\.\d{2}\.\d{4}\.\s+\d{1,2}:\d{2})\)" text
                let author = m.GroupValues.[0].Trim()
                let! created = m.GroupValues.[1]
                               |> LocalDateTimePattern.CreateWithCurrentCulture("dd.MM.yyyy. H:mm").Parse
                               |> Helpers.tryGetLocalDate
                return author, created
            }

        trial {
            let! mark = extractMarkText ()
            let! markDetailsList = rootNode.CssSelect(".mark-card-inner-content p")
                                   |> List.map extractMarkDetails
                                   |> collect

            let! authorDetails = markDetailsList 
                                 |> List.filter (fun d -> d.Topic = "Autors:")
                                 |> Seq.tryHead
                                 |> failIfNone "Failed to extract author details"
            let! author, created = extractAuthorAndCreated authorDetails.Value
                                   |> failIfNone "Failed to extract author and created"

            let! descriptionDetails = markDetailsList 
                                      |> List.filter (fun d -> d.Topic = "Tēma:")
                                      |> Seq.tryHead
                                      |> failIfNone "Failed to extract description details"
            let description = descriptionDetails.Value

            return {
                Id = scoreReference.Id;
                Display = scoreReference.Display;
                Mark = mark;
                Author = author;
                Created = created;
                Description = description;
            }
        }

    let extractMarkFromHtml html scoreReference =
        trial {
            let htmlDocument = "<html><body>" + html + "</body></html>" |> HtmlDocument.Parse
            let! rootElement = htmlDocument.CssSelect("div.mark-card-inner")
                               |> List.toSeq
                               |> Seq.tryHead
                               |> failIfNone "Failed to extract mark details value"
            return! extractMark rootElement scoreReference
        }
