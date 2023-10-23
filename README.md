# Seq App For Azure DevOps Bug Reporting
A Seq App that reports bugs to Azure DevOps and keeps track of issue incidence frequency.

## Requirements
Seq 2023.4 or later.

## Installation
Go to your Seq instance, then settings, then apps, then add a new app and type "Seq.App.Bug.Reporter.AzureDevOps" in the search box. Then click install.

## Configuration
On your app screen, click "Add Instance".

There are some required and optional fields.

### Required Fields
You are required to fill these fields, otherwise the app will not work.

#### Organization
The name of your Azure DevOps organization, this is a required field.

#### Project
The name of your Azure DevOps project, this is a required field.

#### Personal Access Token
A personal access token with access to the project (please make sure that the Read/Write accesses to work items are attributed to the PAT), this is a required field. (DO NOT SET A FULL ACCESS TOKEN HERE, IT's NOT RECOMMENDED FOR SECURITY REASONS).

#### Description Mapping Field
If you're using CMMI use Microsoft.VSTS.CMMI.Symptom. If you're using Scrum you should use Microsoft.VSTS.TCM.ReproSteps.

### Optional Fields
You're not required to fill these.

#### Tags
Comma separated list of bug tags to apply to the created bug in Azure DevOps.

#### Area Path
Area Path of the Azure DevOps bug.

#### Iteration
Iteration of the Azure DevOps bug.

#### Assigned To
Who the work item should be assigned to. If left blank it will default to unassigned.

#### Seq Event Id
Seq Event Id custom Azure DevOps field.

#### Seq Event Url
Azure DevOps custom field to store Seq Event Url.

#### Seq Event Incidence Count
Azure DevOps custom field to store the number of times this bug occurred and been logged in Seq.

#### Unique Incident Id
Azure DevOps custom field to store a unique incident id to prevent the creation of multiple bugs for the same incident, the unique id is a SHA-256 hash of the bug title.

#### Bug Title Format
Provides the title format of the bug. List of parameters that can be used are ({{EventLogLevel}}, {{EventMessage}}, {{EventId}}, {{EventTimestamp}}), you can also use Seq event properties). If left empty, the bug title will be '[SEQ Bug Reporter/{{EventLogLevel}}] - {{EventMessage}}'.
```
[SEQ Bug Reporter/{EventLogLevel}] - {EventMessage}
```

#### Bug Description Format
Provides the description format of the bug. List of parameters that can be used are ({{EventLogLevel}}, {{EventMessage}}, {{EventUrl}}, {{EventId}}, {{EventTimestamp}}, {{EventException}}, {{EventProperties}}), you can also use Seq event properties).
```html
<strong>Event Id:</strong> {EventId}<br/>
<strong>Level:</strong> {EventLogLevel}<br/>
<strong>Timestamp:</strong> {EventTimestamp}<br/>
<strong>Event Url:</strong> <a href="{EventUrl}" target="_blank">Seq event details</a><br/>
{EventProperties}<br />
<strong>Message:</strong> {EventMessage}<br/>
{EventException}<br />
```

#### Parent Work Item Id
Link to the parent related work item, if left blank the created bug will be un-parented.

#### Azure DevOps Props Mappings
Maps Azure DevOps properties to constant values. Format: AzureDevOpsProperty:ConstValue, Separated by Commas. Example: Priority:2,Triage:Level 1

#### Azure DevOps Severity Mappings
Maps Seq Log level to Azure DevOps bug severity. Format: LogLevel:AzureDevOpsSeverity, Separated by Commas. Example: Error:2 - High,Fatal:1 - Critical

## Authors
* Arsslen Idadi [@Arsslensoft](https://github.com/Arsslensoft)

## Credits
The Datalust team for the Seq App template [@datalust](https://github.com/datalust/seq-app-mail)

The project was inspired by [@Seq.App.Azure.DevOps](https://github.com/xantari/Seq.App.Azure.DevOps) which was authored by Matt Olson [@xantari](https://github.com/xantari) and Christopher Baker [@delubear](https://github.com/Delubear).