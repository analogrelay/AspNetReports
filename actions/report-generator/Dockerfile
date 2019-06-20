FROM mcr.microsoft.com/dotnet/core/sdk:3.0.100-preview6

LABEL com.github.actions.name="ASP.NET Core Report Generator"
LABEL com.github.actions.description="Runs the ASP.NET Core Report Generator command"
LABEL com.github.actions.icon="bar-chart"
LABEL com.github.actions.color="blue"

COPY ./entrypoint.sh .

ENTRYPOINT [ "./entrypoint.sh" ]