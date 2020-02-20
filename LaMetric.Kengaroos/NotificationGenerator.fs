namespace LaMetric.Kengaroos

module NotificationGenerator =
    
    open System
    open FSharpx.Option
    open FSharpx.Text
    open NodaTime
    open NodaTime.Text
    open NodaTime.Extensions
    open Newtonsoft.Json
    open Newtonsoft.Json.FSharp
    open Serilog

    open DataTypes
    open LaMetric.Common.DataTypes
    open LaMetric.Common.Icons


    let getDisplayEndDate (today: LocalDate) =
        today.PlusDays(7)

    let fetchParsedCalendar calendarUrl (today: LocalDate) =
        let fetchPageForDate date =
            let url = calendarUrl + LocalDatePattern.CreateWithInvariantCulture("/MM/yyyy").Format(date)
            KengaroosWebClient.fetchPage url |> CalendarDayParser.extractCalendarDaysFromHtml
        
        let isSameMonth (date1: LocalDate) (date2: LocalDate) =
            date1.Year = date2.Year && date1.Month = date2.Month


        let endDate = getDisplayEndDate today

        [ today; today.PlusMonths(1) ]
        |> List.filter (fun d -> isSameMonth d today || isSameMonth d endDate)
        |> List.collect fetchPageForDate
        |> List.distinctBy (fun cd -> cd.Date)

    let getToday () =
        SystemClock.Instance.InTzdbSystemDefaultZone().GetCurrentDate()

    let serializeObject obj = 
        let settings = new JsonSerializerSettings() |> Serialisation.extend
        JsonConvert.SerializeObject(obj, settings)

    let generateFrames (calendarDays: CalendarDay list) (today: LocalDate) =
        let generateFrame calendarDay =
            let diff = calendarDay.Date - today

            let icon = match diff.Days with
                         | 0 -> "i5124"
                         | 1 -> "i5125"
                         | 2 -> "i8567"
                         | 3 -> "i4999"
                         | _ -> "i4998"

            let prefix = match diff.Days with
                         | 0 -> Some "Shodien"
                         | 1 -> Some "Rit"
                         | 2 -> Some "Parit"
                         | _ -> Option.None

            let getlatvianDay (date: LocalDate) =
                match date.DayOfWeek with
                | IsoDayOfWeek.Monday -> "Pirmdien"
                | IsoDayOfWeek.Tuesday -> "Otrdien"
                | IsoDayOfWeek.Wednesday -> "Treshdien"
                | IsoDayOfWeek.Thursday -> "Ceturtdien"
                | IsoDayOfWeek.Friday -> "Piektdien"
                | IsoDayOfWeek.Saturday -> "Sestdien"
                | IsoDayOfWeek.Sunday -> "Svētdien"
                | _ -> ""

            let events = "(" + getlatvianDay calendarDay.Date + ")" +
                         LocalDatePattern.CreateWithInvariantCulture("dd.MM.yyyy: ").Format(calendarDay.Date) +
                         String.Join("; ", calendarDay.Events)

            let text = match prefix with
                       | Some p -> p + " " + events
                       | Option.None -> events

            NotificationSimpleFrame {
                index = Option.None;
                icon = IconId icon |> Some;
                text = text;
            }


        calendarDays
        |> List.filter (fun cd -> cd.Date >= today && cd.Date <= getDisplayEndDate today)
        |> List.sortBy (fun cd -> cd.Date)
        |> List.map generateFrame

    let generateSerializedFrames calendarDays today = 
        let addFrameIndex index frame =
            match frame with
            | NotificationSimpleFrame f -> NotificationSimpleFrame { f with index = Some index }
            | NotificationGoalFrame f -> NotificationGoalFrame f
            | NotificationChartFrame f -> NotificationChartFrame f

        let frames = generateFrames calendarDays today
                     |> List.mapi addFrameIndex
        let nonEmptyFrames = match frames with
                             | [] -> [ NotificationSimpleFrame {
                                            index = Option.None;
                                            icon = IconId "a1496" |> Some; // Pacman
                                            text = "Nav sporta";
                                        }
                                    ]
                             | _ -> frames

        serializeObject { frames = nonEmptyFrames; sound = Option.None; cycles = Option.None }