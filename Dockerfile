FROM mcr.microsoft.com/dotnet/sdk:6.0 as build

WORKDIR /app

COPY . .

RUN dotnet build && \
  dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine

WORKDIR /app

COPY --from=build /app/out .

ENV ASPNETCORE_URLS=http://+:8090
ENV ASPNETCORE_ENVIRONMENT=Production
ENV KENGAROOS_CALENDAR_URL=http://kengaros.lv/index.php/raspisanie
ENV EKLASE_LOGOUT_URL=https://my.e-klase.lv/LogOut
ENV EKLASE_DIARY_URL=https://my.e-klase.lv/Family/Diary
ENV EKLASE_LOGIN_URL=https://my.e-klase.lv/?v=15
ENV EKLASE_MARK_URL=https://my.e-klase.lv/Family/MarkFile/Get

EXPOSE 8090

ENTRYPOINT ["dotnet", "LametricApp.dll"]
