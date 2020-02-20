namespace LaMetric.Kengaroos

module CalendarDayParser =

    open FSharp.Data
    open FSharpx.Option
    open FSharpx.Text
    open NodaTime.Text

    open DataTypes
    open LaMetric.Common.TryParser
    

    let extractOurEvent event =
        maybe {
            let! m = Regex.tryMatch @"2009" event
            return event
        }

    let extractCalendarDays (bodyNode: HtmlNode) =
        let extractCalendarDay (cellNode: HtmlNode) =
            let extractDate () =
                maybe {
                    let! dateLinkNode = cellNode.CssSelect("a[href]")
                                        |> List.filter (fun n -> n.CssSelect(".rs_calendar_date") |> List.isEmpty |> not)
                                        |> Seq.tryHead
                    let! m = dateLinkNode.AttributeValue("href") |> Regex.tryMatch @"/(\d{2}\-\d{2}\-\d{4})$"
                    return! m.GroupValues.[0]
                            |> LocalDatePattern.CreateWithInvariantCulture("MM-dd-yyyy").Parse
                            |> Helpers.tryGetLocalDate
                }

            let extractEvents () =
                maybe {
                    let! eventsNode = cellNode.CssSelect("a.rse_event_link")
                                      |> Seq.tryHead
                    let! text = eventsNode.DirectInnerText() |> parseString
                    let events = text.Split ','
                                 |> Array.map (fun t -> t.Trim())
                                 |> Array.choose extractOurEvent
                                 |> Array.toList

                    return! match events with
                            | [] -> Option.None
                            | _ -> Some events
                }

            maybe {
                let! date = extractDate ()
                let! events = extractEvents ()
                return {
                    Date = date;
                    Events = events;
                }
            }

        bodyNode.CssSelect("td.has-events")
        |> List.choose extractCalendarDay


    let extractCalendarDaysFromHtml html =
        let htmlDocument = HtmlDocument.Parse html
        extractCalendarDays <| htmlDocument.Body()

