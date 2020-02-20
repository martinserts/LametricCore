namespace LaMetric.Eklase.Tests

module DiaryUrlTests =
    open Xunit
    open Swensen.Unquote
    open LaMetric.Eklase.NotificationGenerator

    [<Theory>]
    [<InlineData("12.05.2017;")>]
    [<InlineData("13.05.2017;?Date=15.05.2017.")>]
    [<InlineData("14.05.2017;?Date=15.05.2017.")>]
    let ``sunday moves to next monday`` (data : string) =
        let parts = data.Split ';'

        let dateFormat = "dd.MM.yyyy"
        let datePattern = NodaTime.Text.LocalDatePattern.CreateWithInvariantCulture(dateFormat)
        let startDate = datePattern.Parse(parts.[0]).Value

        let diaryUrl = "URL"

        let fullUrl = getFullDiaryUrl diaryUrl startDate
        let expectedUrl = diaryUrl + parts.[1]


        test <@ fullUrl = expectedUrl @>

