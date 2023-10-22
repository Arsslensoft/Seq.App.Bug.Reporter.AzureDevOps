using System.Text;
using Seq.App.BugReporter.AzureDevOps.Constants;
using Seq.App.BugReporter.AzureDevOps.Resources;
using Seq.Apps;
using Seq.Apps.LogEvents;
using Serilog;
using Serilog.Parsing;

namespace Seq.App.BugReporter.AzureDevOps.Formatters;

/// <summary>
/// Represents a parameterized string formatter for Seq.
/// </summary>
public class ParameterizedSeqStringFormatter
{
    private readonly Event<LogEventData> _event;


    public ParameterizedSeqStringFormatter(Event<LogEventData> @event)
    {
        _event = @event;
    }

    public string? GetTitle(string? titleTemplate)
    {
        return FormatTemplate(string.IsNullOrEmpty(titleTemplate)
            ? Strings.DEFAULT_TITLE
            : titleTemplate);
    }

    public string? GetDescription(string baseUrl, string? descriptionTemplate)
    {
        return FormatTemplate(string.IsNullOrEmpty(descriptionTemplate)
            ? Strings.DEFAULT_DESCRIPTION
            : descriptionTemplate, baseUrl);
    }

    public string? FormatTemplate(string template, string? baseUrl = null, bool isMinifiedTemplate = true)
    {
        Log.BindMessageTemplate(template, _event.Data.Properties.Select(p => p.Value).ToArray(),
            out var boundTemplate, out _);

        if (boundTemplate == null) return null;

        var sb = new StringBuilder();
        foreach (var tok in boundTemplate.Tokens)
        {
            var tokenString = tok.ToString();

            if (tok is TextToken)
                sb.Append(tok);
            else
                FormatParameter(sb, tokenString, baseUrl, isMinifiedTemplate);
        }

        return sb.ToString();
    }

    private void FormatParameter(StringBuilder sb, string? tokenString, string? baseUrl, bool isMinifiedTemplate)
    {
        switch (tokenString)
        {
            case ParameterConstants.EventId:
                sb.Append(_event.Id);
                break;
            case ParameterConstants.EventMessage when _event.Data != null:
                sb.Append(_event.Data.RenderedMessage);
                break;
            case ParameterConstants.EventLogLevel when _event.Data != null:
                sb.Append(_event.Data.Level);
                break;
            case ParameterConstants.EventTimestamp when _event.Data != null:
                sb.Append(_event.Data.LocalTimestamp.ToLocalTime());
                break;
            case ParameterConstants.EventUrl when _event.Data != null && baseUrl != null && !isMinifiedTemplate:
                sb.Append(GetSeqUrl(baseUrl));
                break;
            case ParameterConstants.EventException when _event.Data?.Exception != null && !isMinifiedTemplate:
            {
                sb.Append(
                    $"<strong>{Strings.EXCEPTION}:</strong><p style=\"background-color: #921b3c; color: white; border-left: 8px solid #7b1e38;\">{_event.Data.Exception}</p>");
                break;
            }
            case ParameterConstants.EventProperties when _event.Data != null && !isMinifiedTemplate:
            {
                foreach (var m in _event.Data.Properties.Keys)
                    sb.Append($"<strong>{m}</strong>: {_event.Data.Properties[m]} <br/>");

                break;
            }
            case not null when _event.Data?.Properties != null:
            {
                sb.Append(_event.Data?.Properties[tokenString.Replace("{", "").Replace("}", "")]);
                break;
            }
            default:
                sb.Append("UNSUPPORTED_PARAMETER");
                break;
        }
    }

    public string GetSeqUrl(string baseUrl)
    {
        return $"{baseUrl}#/events?filter=@Id%20%3D%20'{_event.Id}'&show=expanded";
    }
}