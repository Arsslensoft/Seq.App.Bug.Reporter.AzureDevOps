using System.Text;
using Seq.App.Bug.Reporter.AzureDevOps.Constants;
using Seq.App.Bug.Reporter.AzureDevOps.Resources;
using Seq.Apps;
using Seq.Apps.LogEvents;
using Serilog;
using Serilog.Parsing;

namespace Seq.App.Bug.Reporter.AzureDevOps.Formatters;

/// <summary>
/// Represents a parameterized string formatter for Seq.
/// </summary>
public class ParameterizedSeqStringFormatter
{
    private readonly Event<LogEventData> _event;
    private readonly ILogger _logger;

    /// <summary>
    /// Creates a new instance of <see cref="ParameterizedSeqStringFormatter"/>.
    /// </summary>
    /// <param name="event">The log event</param>
    /// <param name="logger">The current logger</param>
    public ParameterizedSeqStringFormatter(Event<LogEventData> @event, ILogger logger)
    {
        _event = @event;
        _logger = logger;
    }

    /// <summary>
    /// Gets the title of the bug.
    /// </summary>
    /// <param name="titleTemplate">The user-defined title format</param>
    /// <returns>The formatted title</returns>
    public string? GetTitle(string? titleTemplate)
    {
        return FormatTemplate(string.IsNullOrEmpty(titleTemplate)
            ? Strings.DEFAULT_TITLE
            : titleTemplate);
    }

    /// <summary>
    /// Gets the description of the bug.
    /// </summary>
    /// <param name="baseUrl">The Seq base url</param>
    /// <param name="descriptionTemplate">The user-defined description format</param>
    /// <returns>The formatted description</returns>
    public string? GetDescription(string? baseUrl, string? descriptionTemplate)
    {
        return FormatTemplate(string.IsNullOrEmpty(descriptionTemplate)
            ? Strings.DEFAULT_DESCRIPTION
            : descriptionTemplate, baseUrl, false);
    }

    /// <summary>
    /// Gets the Seq url of the event.
    /// </summary>
    /// <param name="baseUrl">The Seq base url</param>
    /// <returns>The Seq url of the log event</returns>
    public string GetSeqUrl(string baseUrl)
    {
        return $"{baseUrl}#/events?filter=@Id%20%3D%20'{_event.Id}'&show=expanded";
    }

    private string? FormatTemplate(string template, string? baseUrl = null, bool isMinifiedTemplate = true)
    {
        _logger.BindMessageTemplate(template, _event.Data?.Properties?.Select(p => p.Value).ToArray() ?? Array.Empty<object?>(),
            out var boundTemplate, out _);

        if (boundTemplate == null) return null;

        var messageBuilder = new StringBuilder();
        foreach (var tok in boundTemplate.Tokens)
        {
            if (tok is TextToken)
                messageBuilder.Append(tok);
            else
                FormatParameter(messageBuilder, tok, baseUrl, isMinifiedTemplate);
        }

        return messageBuilder.ToString();
    }

    private void FormatParameter(StringBuilder messageBuilder, MessageTemplateToken token, string? baseUrl, bool isMinifiedTemplate)
    {
        if (token is not PropertyToken {PropertyName: { } propertyName}) return;

        switch (propertyName)
        {
            case ParameterConstants.EventId:
                messageBuilder.Append(_event.Id);
                break;
            case ParameterConstants.EventMessage when _event.Data != null:
                messageBuilder.Append(_event.Data.RenderedMessage);
                break;
            case ParameterConstants.EventLogLevel when _event.Data != null:
                messageBuilder.Append(_event.Data.Level);
                break;
            case ParameterConstants.EventTimestamp when _event.Data != null:
                messageBuilder.Append(_event.Data.LocalTimestamp.ToLocalTime());
                break;
            case ParameterConstants.EventUrl when _event.Data != null && baseUrl != null && !isMinifiedTemplate:
                messageBuilder.Append(GetSeqUrl(baseUrl));
                break;
            case ParameterConstants.EventException when _event.Data?.Exception != null && !isMinifiedTemplate:
            {
                messageBuilder.Append(
                    $"<strong>{Strings.EXCEPTION}:</strong><p style=\"background-color: #921b3c; color: white; border-left: 8px solid #7b1e38;\">{_event.Data.Exception}</p>");
                break;
            }
            case ParameterConstants.EventProperties when _event.Data != null && !isMinifiedTemplate:
            {
                foreach (var m in _event.Data.Properties.Keys)
                    messageBuilder.Append($"<strong>{m}</strong>: {_event.Data.Properties[m]} <br/>");

                break;
            }
            case not null when _event.Data?.Properties != null && _event.Data.Properties.ContainsKey(propertyName):
            {
                messageBuilder.Append(_event.Data?.Properties[propertyName]);
                break;
            }
            default:
                messageBuilder.Append(string.Format(Strings.UNSUPPORTED_PARAMETER, propertyName));
                break;
        }
    }
}