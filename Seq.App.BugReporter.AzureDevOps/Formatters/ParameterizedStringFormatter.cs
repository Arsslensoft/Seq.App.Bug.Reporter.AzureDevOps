using System.Text;
using Seq.Apps;
using Seq.Apps.LogEvents;
using Serilog;
using Serilog.Parsing;
using Seq.App.BugReporter.AzureDevOps.Constants;

namespace Seq.App.BugReporter.AzureDevOps.Formatters
{
	public class ParameterizedSeqStringFormatter
	{
		private readonly Event<LogEventData> _event;

		public ParameterizedSeqStringFormatter(Event<LogEventData> @event)
		{
			_event = @event;
		}

		public string? GetTitle(string? titleTemplate)
		{
			if(string.IsNullOrEmpty(titleTemplate)) 
			{
        		return FormatTemplate($"[SEQ Bug Reporter/{{{ParameterConstants.EventLogLevel}}}] - {{{ParameterConstants.EventMessage}}}");
			}
 			
			return FormatTemplate(titleTemplate);
		}
		public string? GetDescription(string baseUrl, string descriptionTemplate)
		{
			if(string.IsNullOrEmpty(descriptionTemplate)) 
			{
				var sb = new StringBuilder();
        		sb.Append($"<strong>Event Id:</strong> {{{ParameterConstants.EventId}}}<br/>");
        		sb.Append($"<strong>Level:</strong> {{{ParameterConstants.EventLogLevel}}}<br/>");
        		sb.Append($"<strong>Timestamp:</strong> {{{ParameterConstants.EventTimestamp}}}<br/>");
        		sb.Append($"<strong>Event Url:</strong> <a href=\"{{{ParameterConstants.EventUrl}}}\" target=\"_blank\">Seq event details</a><br/>");
        		sb.Append($"{{{ParameterConstants.EventProperties}}}<br />");
        		sb.Append($"<strong>Message:</strong> {{{ParameterConstants.EventMessage}}}<br/>");
        		sb.Append($"{{{ParameterConstants.EventException}}}<br />");

        		return FormatTemplate(sb.ToString(), baseUrl);
			}
 			
			return FormatTemplate(descriptionTemplate, baseUrl);
		}

		public string? FormatTemplate(string template, string? baseUrl = null, bool isMinifiedTemplate = true) 
		{
			Log.BindMessageTemplate(template, _event.Data.Properties.Select(p => p.Value).ToArray(),
            	out var boundTemplate, out _);

        	if(boundTemplate == null) return null;

        	var sb = new StringBuilder();
        	foreach (var tok in boundTemplate.Tokens)
        	{
        	    var tokenString = tok.ToString();

        	    if (tok is TextToken)
        	        sb.Append(tok);
        	    else
        	    {
        	        if (tokenString == "EventId")
        	            sb.Append(_event.Id);
	
        	        if(_event.Data == null)
        	        {
        	            sb.Append("UNRESOLVED_SEQ_TOKEN");
        	            continue;
        	        }

					if(tokenString == ParameterConstants.EventMessage)
						sb.Append(_event.Data.RenderedMessage);
	
        	        if (tokenString == ParameterConstants.EventLogLevel)
        	            sb.Append(_event.Data.Level);
        	        if (tokenString == ParameterConstants.EventTimestamp)
        	            sb.Append(_event.Data.LocalTimestamp.ToLocalTime());


					// Skip the big part
					if(isMinifiedTemplate) 
					{
						sb.Append("UNSUPPORTED_PARAMETER");
						continue;
					}
					
					if (tokenString == ParameterConstants.EventUrl && baseUrl != null)
        	            sb.Append(GetSeqUrl(baseUrl));

        	        if (tokenString == ParameterConstants.EventException)
        	        {
        	            if (_event.Data?.Exception != null)
        	                sb.AppendFormat(
        	                    "<strong>Exception:</strong><p style=\"background-color: #921b3c; color: white; border-left: 8px solid #7b1e38;\">{0}</p>",
        	                    _event.Data.Exception);
        	        }

        	        if (tokenString == ParameterConstants.EventProperties)
        	        {
        	            foreach (var m in _event.Data.Properties.Keys)
        	            {
        	                sb.Append($"<strong>{m}</strong>: {_event.Data.Properties[m]} <br/>");
        	            }
        	        }
        	        else if(_event.Data?.Properties != null && tokenString != null)
        	            sb.Append(_event.Data?.Properties[tokenString.Replace("{", "").Replace("}", "")]);
        	    }
        	}

        	return sb.ToString();
		}

		public string GetSeqUrl(string baseUrl)
    	{
    	    return $"{baseUrl}#/events?filter=@Id%20%3D%20'{_event.Id}'&show=expanded";
    	}
	}
}

