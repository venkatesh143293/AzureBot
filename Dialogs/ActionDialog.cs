// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.BotBuilderSamples.Dialogs
{


    public class ActionDialog : CancelAndHelpDialog
    {
        protected readonly IConfiguration Configuration;
        protected readonly ILogger Logger;
        public ActionDialog(IConfiguration configuration, ILogger<MainDialog> logger)
            : base(nameof(ActionDialog))
        {
            Configuration = configuration;
            Logger = logger;
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new DateResolverDialog());
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                DestinationStepAsync,
                PortfolioStepAsync,
                EnvironmentStepAsync,
                TagStepAsync,
                DbInstanceStepAsync,
                ConfirmStepAsync,
                CaptureEmailStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }
        private async Task<DialogTurnResult> DestinationStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        
        {
            var entitiDetails = (EntitiDetails)stepContext.Options;
            if (entitiDetails.Intent == "Acronym")
            {
                if (entitiDetails.Acronym == null && entitiDetails.Score > 0.3)
                {
                    return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Which acronym details would you like to have?") }, cancellationToken);
                }
                if (entitiDetails.Score < 0.3 && entitiDetails.Acronym == null)
                {
                    return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Sorry I didn't get you. Please try other word") }, cancellationToken);
                }
                else
                {
                    return await stepContext.EndDialogAsync(entitiDetails, cancellationToken);
                }
            }
            else if (entitiDetails.Intent == "Trigger_Service" || entitiDetails.Intent == "Build_Deployment")
            {
                if ((entitiDetails.Project == null || entitiDetails.Project == "ProductionDailyHealthCheck" || entitiDetails.Portfolio == null) && entitiDetails.Score > 0.3 && entitiDetails.Portfolio == null)
                {
                    return await stepContext.PromptAsync(nameof(ChoicePrompt),
                     new PromptOptions
                     {
                         Prompt = MessageFactory.Text("Please enter the portfolio"),
                         Choices = ChoiceFactory.ToChoices(new List<string> { "PCA", "CCV", "Rapid" }),
                         RetryPrompt = MessageFactory.Text("Sorry, I'm still learning. Please provide the valid option or below mentioned Sequence Number."),
                     }, cancellationToken);

                }
                else if (entitiDetails.Score < 0.3 && entitiDetails.Acronym == null)
                {
                    return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Sorry I didn't get you. Please try other Project") }, cancellationToken);
                }
                else
                {
                    return await stepContext.NextAsync(entitiDetails.Project, cancellationToken);
                }
            }
            else
            {
                return await stepContext.NextAsync(entitiDetails.Project, cancellationToken);
            }

        }
        private async Task<DialogTurnResult> PortfolioStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var entitiDetails = (EntitiDetails)stepContext.Options;
            switch (entitiDetails.Intent)
            {
                case "Acronym":
                    return await stepContext.NextAsync(entitiDetails.Acronym, cancellationToken);
                case "Trigger_Service":
                    if (entitiDetails.Portfolio == null) entitiDetails.Portfolio = entitiDetails.Portfolio = ((FoundChoice)stepContext.Result).Value.ToString();
                    if (!string.IsNullOrEmpty(entitiDetails.Project))
                    {
                        if (entitiDetails.Tag == "AutoDerivation" && string.IsNullOrEmpty(entitiDetails.Environment) || entitiDetails.Tag == "CommitFile")
                        {
                            return await stepContext.PromptAsync(nameof(ChoicePrompt),
                             new PromptOptions
                             {
                                 Prompt = MessageFactory.Text("Please select the environment"),
                                 Choices = ChoiceFactory.ToChoices(new List<string> { "DEV", "QA", "UAT", "PROD" }),
                                 Style = ListStyle.Auto,
                                 RetryPrompt = MessageFactory.Text("Sorry, I'm still learning. Please provide the valid option or below mentioned Sequence Number."),
                             }, cancellationToken);
                        }
                        if (entitiDetails.Project.ToUpper() == "PRODUCTIONDAILYHEALTHCHECK" && string.IsNullOrEmpty(entitiDetails.Portfolio))
                            entitiDetails.Portfolio = ((FoundChoice)stepContext.Result).Value.ToString();
                        return await stepContext.NextAsync(entitiDetails, cancellationToken);
                    }

                    //if (entitiDetails.Project == "ProductionDailyHealthCheck")
                    //    return await stepContext.NextAsync(entitiDetails, cancellationToken);
                    if (entitiDetails.Portfolio.ToUpper() == "PCA")
                    {

                        return await stepContext.PromptAsync(nameof(ChoicePrompt),
               new PromptOptions
               {
                   Prompt = MessageFactory.Text("Please select the project"),
                   Choices = GetProjectChoices(entitiDetails.Portfolio),
                   Style = ListStyle.List,
                   RetryPrompt = MessageFactory.Text("Sorry, I'm still learning. Please provide the valid option or below mentioned Sequence Number."),
               }, cancellationToken);
                    }
                    else if (entitiDetails.Portfolio.ToUpper() == "CCV")
                    {

                        return await stepContext.PromptAsync(nameof(ChoicePrompt),
               new PromptOptions
               {
                   Prompt = MessageFactory.Text("Please select the project"),
                   Choices = GetProjectChoices(entitiDetails.Portfolio),
                   Style = ListStyle.Auto,
                   RetryPrompt = MessageFactory.Text("Sorry, I'm still learning. Please provide the valid option or below mentioned Sequence Number."),
               }, cancellationToken);
                    }
                    //else if (entitiDetails.Tag.Contains("AutoDerivation"))
                    //{

                    //    return await stepContext.NextAsync(entitiDetails, cancellationToken);
                    //}
                    else
                    {
                        return await stepContext.PromptAsync(nameof(ChoicePrompt),
              new PromptOptions
              {
                  Prompt = MessageFactory.Text("Please select the project"),
                  Choices = GetProjectChoices(entitiDetails.Portfolio),
                  Style = ListStyle.Auto,
                  RetryPrompt = MessageFactory.Text("Sorry, I'm still learning. Please provide the valid option or below mentioned Sequence Number."),
              }, cancellationToken);
                    }
                case "Build_Deployment":
                    if (!string.IsNullOrEmpty(entitiDetails.Project))
                        return await stepContext.NextAsync(entitiDetails, cancellationToken);
                    //entitiDetails.Portfolio = entitiDetails.Portfolio = ((FoundChoice)stepContext.Result).Value.ToString();
                    return await stepContext.PromptAsync(nameof(ChoicePrompt),
              new PromptOptions
              {
                  Prompt = MessageFactory.Text("Please select the project"),
                  Choices = GetProjectChoices(entitiDetails.Intent),
                  Style = ListStyle.Auto,
                  RetryPrompt = MessageFactory.Text("Sorry, I'm still learning. Please provide the valid option or below mentioned Sequence Number."),
              }, cancellationToken);
                default:
                    return await stepContext.NextAsync(entitiDetails.Project, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> EnvironmentStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var entitiDetails = (EntitiDetails)stepContext.Options;
            switch (entitiDetails.Intent)
            {
                case "Acronym":
                    return await stepContext.NextAsync(entitiDetails.Acronym, cancellationToken);
                case "Trigger_Service":
                    if (entitiDetails.Portfolio == "PCA" || entitiDetails.Portfolio == "CCV")
                    {

                        if (entitiDetails.Tag == "CommitFile")
                            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please enter the MUV Key") }, cancellationToken);
                        if (entitiDetails.Tag == "AutoDerivation" && string.IsNullOrEmpty(entitiDetails.Environment))
                        {
                            entitiDetails.Environment = ((FoundChoice)stepContext.Result).Value.ToString();
                            return await stepContext.PromptAsync(nameof(ChoicePrompt),
                             new PromptOptions
                             {
                                 Prompt = MessageFactory.Text("Please confirm your selection"),
                                 Choices = ChoiceFactory.ToChoices(new List<string> { "Immediate", "Schedule" }),
                                 RetryPrompt = MessageFactory.Text("Sorry, I'm still learning. Please provide the valid option or below mentioned Sequence Number."),
                             }, cancellationToken);
                        }
                        if (string.IsNullOrEmpty(entitiDetails.Project))
                            entitiDetails.Project = ((FoundChoice)stepContext.Result).Value.ToString();
                        if(string.IsNullOrEmpty(entitiDetails.Environment) && entitiDetails.Project.ToUpper() != "PRODUCTIONDAILYHEALTHCHECK")
                        {
                            return await stepContext.PromptAsync(nameof(ChoicePrompt),
                            new PromptOptions
                            {
                                Prompt = MessageFactory.Text("Please select the environment"),
                                Choices = ChoiceFactory.ToChoices(new List<string> { "DEV", "QA","UAT" }),
                                RetryPrompt = MessageFactory.Text("Sorry, I'm still learning. Please provide the valid option or below mentioned Sequence Number."),
                            }, cancellationToken);
                        }
                        return await stepContext.NextAsync(entitiDetails, cancellationToken);
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(entitiDetails.Project))
                        {
                            entitiDetails.Project = ((FoundChoice)stepContext.Result).Value.ToString();
                            return await stepContext.PromptAsync(nameof(ChoicePrompt),
                                          new PromptOptions
                                          {
                                              Prompt = MessageFactory.Text("Please select the environment"),
                                              Choices = (entitiDetails.Project != "Annocoder") ? GetRapidEnvironments("Environment") : GetAnnocoderEnvironments("Environment"),
                                              //Choices = GetRapidEnvironments("Environment"), //ChoiceFactory.ToChoices(new List<string> { "QA Backend", "QA Meca", "UAT", "PROD" }),
                                              RetryPrompt = MessageFactory.Text("Sorry, I'm still learning. Please provide the valid option or below mentioned Sequence Number."),
                                          }, cancellationToken);
                        }
                        else
                            return await stepContext.NextAsync(entitiDetails, cancellationToken);
                    }
                case "Build_Deployment":
                    if (!string.IsNullOrEmpty(entitiDetails.Project) && !string.IsNullOrEmpty(entitiDetails.Environment))
                        return await stepContext.NextAsync(entitiDetails, cancellationToken);
                    else if (string.IsNullOrEmpty(entitiDetails.Project))
                        entitiDetails.Project = ((FoundChoice)stepContext.Result).Value.ToString();
                    if (entitiDetails.Project == "App-Deployment" )
                        return await stepContext.PromptAsync(nameof(ChoicePrompt),
                                new PromptOptions
                                {
                                    Prompt = MessageFactory.Text("Please select the environment"),
                                    Choices = ChoiceFactory.ToChoices(new List<string> { "QA", "SPTE", "DEMO" }),
                                    RetryPrompt = MessageFactory.Text("Sorry, I'm still learning. Please provide the valid option or below mentioned Sequence Number."),
                                }, cancellationToken);
                    else
                        return await stepContext.PromptAsync(nameof(ChoicePrompt),
                           new PromptOptions
                           {
                               Prompt = MessageFactory.Text("Please select the environment"),
                               Choices = ChoiceFactory.ToChoices(new List<string> { "QA(VPMTST1)" }),
                               RetryPrompt = MessageFactory.Text("Sorry, I'm still learning. Please provide the valid option or below mentioned Sequence Number."),
                           }, cancellationToken);
                default:
                    return await stepContext.NextAsync(entitiDetails.Project, cancellationToken);



            }
        }
        private async Task<DialogTurnResult> TagStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var entitiDetails = (EntitiDetails)stepContext.Options;
            if (!string.IsNullOrEmpty(entitiDetails.Project) && !string.IsNullOrEmpty(entitiDetails.Tag) && entitiDetails.Intent == "Trigger_Service")
            {
                if (string.IsNullOrEmpty(entitiDetails.Environment))
                    entitiDetails.Environment = ((FoundChoice)stepContext.Result).Value.ToString();
                if (entitiDetails.Tag == "CommitFile")
                    entitiDetails.MuvKey = (string)stepContext.Result;
                if (entitiDetails.Tag == "AutoDerivation")
                {
                    entitiDetails.ScheduledOption = ((FoundChoice)stepContext.Result).Value.ToString();
                    if (entitiDetails.ScheduledOption == "Schedule")
                    {
                        return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please enter the scheduling Date and Time in EST") }, cancellationToken);
                    }
                }
                return await stepContext.NextAsync(entitiDetails, cancellationToken);
            }
            if (!string.IsNullOrEmpty(entitiDetails.Project) && !string.IsNullOrEmpty(entitiDetails.Buildwar) && entitiDetails.Intent == "Build_Deployment")
                return await stepContext.NextAsync(entitiDetails, cancellationToken);
            else if (!string.IsNullOrEmpty(entitiDetails.Project) && string.IsNullOrEmpty(entitiDetails.Tag))
                if (entitiDetails.Project.ToUpper() == "PRODUCTIONDAILYHEALTHCHECK")
                {
                    return await stepContext.NextAsync(entitiDetails, cancellationToken);
                }
            var bookingDetails = entitiDetails;
            if (entitiDetails.Intent == "Acronym")
            {
                entitiDetails.Acronym = (string)stepContext.Result;
                return await stepContext.NextAsync(entitiDetails.Acronym, cancellationToken);
            }
            else if (entitiDetails.Intent == "Trigger_Service")
            {
                if (entitiDetails.Portfolio == "PCA" || entitiDetails.Portfolio == "CCV")
                {
                    if (entitiDetails.Project == null)
                        entitiDetails.Project = ((FoundChoice)stepContext.Result).Value.ToString();//(string)stepContext.Result;
                    else if(string.IsNullOrEmpty(entitiDetails.Environment))
                        entitiDetails.Environment = ((FoundChoice)stepContext.Result).Value.ToString();
                    if (entitiDetails.Tag == null && entitiDetails.Project.ToUpper() != "PRODUCTIONDAILYHEALTHCHECK")
                    {
                        if (entitiDetails.Project.ToUpper() == "CLIENTPROFILE")
                        {
                            return await stepContext.PromptAsync(nameof(ChoicePrompt),
                             new PromptOptions
                             {
                                 Prompt = MessageFactory.Text("Please select the test"),
                                 Choices = ChoiceFactory.ToChoices(new List<string> { "Smoke", "Regression", "DerivationLogic", "DMUVFunctionality" }),
                                 RetryPrompt = MessageFactory.Text("Sorry, I'm still learning. Please provide the valid option or below mentioned Sequence Number."),
                             }, cancellationToken);
                        }
                        else
                        {
                            return await stepContext.PromptAsync(nameof(ChoicePrompt),
                             new PromptOptions
                             {
                                 Prompt = MessageFactory.Text("Please select the test"),
                                 Choices = ChoiceFactory.ToChoices(new List<string> { "Smoke", "Regression","Custom"}),
                                 RetryPrompt = MessageFactory.Text("Sorry, I'm still learning. Please provide the valid option or below mentioned Sequence Number."),
                             }, cancellationToken);
                        }
                    }
                }
                else
                {
                    entitiDetails.Environment = ((FoundChoice)stepContext.Result).Value.ToString();
                    if (entitiDetails.Environment != "PROD")
                    {
                        return await stepContext.PromptAsync(nameof(ChoicePrompt),
                           new PromptOptions
                           {
                               Prompt = MessageFactory.Text("Please select the Client"),
                               Choices = (entitiDetails.Project != "Annocoder") ? GetRapidEnvironments("Client") : GetAnnocoderEnvironments("Client"),
                               //Choices = (entitiDetails.Project != "Annocoder")?GetRapidEnvironments("Client"),// ChoiceFactory.ToChoices(new List<string> { "ROC", "Humana" }),
                               RetryPrompt = MessageFactory.Text("Sorry, I'm still learning. Please provide the valid option or below mentioned Sequence Number."),
                           }, cancellationToken);
                    }
                }

            }
            else
            {
                if (entitiDetails.Environment == null)
                    entitiDetails.Environment = ((FoundChoice)stepContext.Result).Value.ToString();
                if (entitiDetails.Project == "App-Deployment")
                    return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please enter war to deploy the build.\n\n" + " Ex:Ipp-Portal:<version>,Loginservice:<version>,Client-Profile:<version>") }, cancellationToken);
                else if (entitiDetails.Project == "RMI-Deployment")
                    entitiDetails.HostName = "usdtrmi03";
                else
                    return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please enter Sql script path to deploy") }, cancellationToken);
            }
            return await stepContext.NextAsync(entitiDetails, cancellationToken);
        }

        private async Task<DialogTurnResult> DbInstanceStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var entitiDetails = (EntitiDetails)stepContext.Options;
            switch (entitiDetails.Intent)
            {
                case "Acronym":
                    return await stepContext.NextAsync(entitiDetails.Acronym, cancellationToken);
                case "Build_Deployment":

                    if (entitiDetails.Project == "DB-Deployment")
                    {
                        if (entitiDetails.Buildwar == null)
                            entitiDetails.Buildwar = (string)stepContext.Result;
                        entitiDetails.DbInstance = entitiDetails.Environment;
                        return await stepContext.NextAsync(entitiDetails, cancellationToken);
                        //return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please enter DB Instance name") }, cancellationToken);
                    }
                   else if (entitiDetails.Project == "RMI-Deployment")
                    {           
                        entitiDetails.DbInstance = "VPMTST1";
                        return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please enter the repository for deployment") }, cancellationToken);
                    }
                    else
                    {
                        if (entitiDetails.Buildwar == null)
                            entitiDetails.Buildwar = (string)stepContext.Result;
                        return await stepContext.NextAsync(entitiDetails, cancellationToken);
                    }
                case "Trigger_Service":
                    if (entitiDetails.Portfolio == "PCA" || entitiDetails.Portfolio == "CCV")
                    {
                        if (entitiDetails.Tag == null && entitiDetails.Project.ToUpper() != "PRODUCTIONDAILYHEALTHCHECK")
                            entitiDetails.Tag = ((FoundChoice)stepContext.Result).Value.ToString();
                        if (entitiDetails.Tag == "DerivationLogic")
                        {
                            return await stepContext.PromptAsync(nameof(ChoicePrompt),
                             new PromptOptions
                             {
                                 Prompt = MessageFactory.Text("Please select the test"),
                                 Choices = ChoiceFactory.ToChoices(new List<string> { "DMUVDerivationForSingleSource", "DMUVDerivationForMultipleSources", "SubsetDerivationForSingleSource", "SubsetDerivationForMultipleSources" }),
                                 RetryPrompt = MessageFactory.Text("Sorry, I'm still learning. Please provide the valid option or below mentioned Sequence Number."),
                             }, cancellationToken);
                        }
                        else if (entitiDetails.Tag == "DMUVFunctionality")
                        {
                            return await stepContext.PromptAsync(nameof(ChoicePrompt),
                             new PromptOptions
                             {
                                 Prompt = MessageFactory.Text("Please select the test"),
                                 Choices = ChoiceFactory.ToChoices(new List<string> { "DateBand", "NoChange", "Subset", "SuperPayerDelta", "IndividualDelta", "POS", "CMUS", "MUE/NonMUE", "Undo", "NonMUEStateMedicaid", "SelectAllBulkActions" }),
                                 RetryPrompt = MessageFactory.Text("Sorry, I'm still learning. Please provide the valid option or below mentioned Sequence Number."),
                             }, cancellationToken);
                        }
                        else if (entitiDetails.Tag == "Custom")
                        {
                            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please enter tag for customized execution.\n\n" + "For Single Ex: @Tag , For Multiple Ex: @Tag1,@Tag2") }, cancellationToken);
                        }
                        else if (entitiDetails.Tag == "AutoDerivation")
                        {
                            var entitiDetails1 = stepContext.Result != null
                                ?
                            await LuisHelper.ExecuteLuisQuery(Configuration, Logger, stepContext.Context, cancellationToken)
                                 :
                            new EntitiDetails();
                            var eDetails = (EntitiDetails)entitiDetails1;
                            entitiDetails.TravelDate = eDetails.TravelDate;
                        }

                        return await stepContext.NextAsync(entitiDetails, cancellationToken);
                    }
                    else
                    {
                        if (entitiDetails.Client == null && (entitiDetails.Project.ToUpper() != "PRODUCTIONDAILYHEALTHCHECK"))
                        {
                            if (/*entitiDetails.Project.ToUpper() != "ANNOCODER"&&*/ entitiDetails.Environment != "PROD")
                                entitiDetails.Client = ((FoundChoice)stepContext.Result).Value.ToString();
                            return await stepContext.PromptAsync(nameof(ChoicePrompt),
                                new PromptOptions
                                {
                                    Prompt = MessageFactory.Text("Please select the test"),
                                    Choices = ChoiceFactory.ToChoices((entitiDetails.Project.ToUpper() == "ANNOCODER") ? new List<string> { "Regression" } : new List<string> { "Smoke", "Regression" }),
                                    RetryPrompt = MessageFactory.Text("Sorry, I'm still learning. Please provide the valid option or below mentioned Sequence Number."),
                                }, cancellationToken);
                        }
                        else
                            return await stepContext.NextAsync(entitiDetails, cancellationToken);
                    }
                default:
                    return await stepContext.NextAsync(entitiDetails, cancellationToken);
            }
        }
        private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var entitiDetails = (EntitiDetails)stepContext.Options;
            string msg = string.Empty;
            switch (entitiDetails.Intent)
            {
                case "Acronym":
                    if (!string.IsNullOrEmpty(entitiDetails.Acronym))
                    {
                        entitiDetails.Acronym = (string)stepContext.Result;
                        return await stepContext.NextAsync(entitiDetails.Acronym, cancellationToken);
                    }
                    else
                    {
                        return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Sorry I didn't get you. Please try other word/Execute a service") }, cancellationToken);
                    }
                case "Trigger_Service":
                    if (entitiDetails.Portfolio == "Rapid" && entitiDetails.Project.ToUpper() != "PRODUCTIONDAILYHEALTHCHECK") entitiDetails.Tag = ((FoundChoice)stepContext.Result).Value.ToString();
                    if (entitiDetails.Project.ToUpper() == "PRODUCTIONDAILYHEALTHCHECK")
                    {
                        msg = $"Please confirm, Do you want to execute {entitiDetails.Portfolio} {" "}  {entitiDetails.Project} ?";
                        entitiDetails.Tag = "PRODSmokeHealthCheck";
                        if (entitiDetails.Portfolio.ToUpper() == "RAPID")
                            entitiDetails.Project = "RetrievalManagement";
                        else if (entitiDetails.Portfolio.ToUpper() == "CCV")
                        {
                            entitiDetails.Project = "CCVPROD";
                            entitiDetails.Tag = "PRODCITSmoke";
                        }
                        return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = MessageFactory.Text(msg) }, cancellationToken);
                    }
                    else if (entitiDetails.Tag == "AutoDerivation")
                    {
                        entitiDetails.Tag = entitiDetails.Environment + entitiDetails.Tag;
                        if (entitiDetails.ScheduledOption == "Schedule")
                        {
                            if (!entitiDetails.TravelDate.Contains(":"))
                                entitiDetails.TravelDate = entitiDetails.TravelDate + ":00";
                            DateTime endTime = DateTime.Parse(entitiDetails.TravelDate.Replace("T", " "));
                            DateTime strtTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"));
                            /*TimeZoneInfo timeZone1 = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                            TimeZoneInfo timeZone2 = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                            DateTime newTime = (TimeZoneInfo.ConvertTime(estTime, timeZone1, timeZone2));*/
                            String endDate = endTime.ToString("yyyy-MM-dd HH:mm");
                            string startDate = strtTime.ToString("yyyy-MM-dd HH:mm");
                            DateTime dateTime1 = DateTime.Parse(startDate);
                            DateTime dateTime2 = DateTime.Parse(endDate);
                            entitiDetails.JenkinsDelay = (dateTime2 - dateTime1).TotalSeconds.ToString();
                            entitiDetails.TravelDate = endTime.ToString();
                            msg = $"Please confirm, Do you want to execute  {entitiDetails.Tag} for Max Units Visits at {entitiDetails.TravelDate} EST?";
                        }
                        else
                            msg = $"Please confirm, Do you want to execute  {entitiDetails.Tag} for Max Units Visits ?";
                        return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = MessageFactory.Text(msg) }, cancellationToken);
                    }
                    else if (entitiDetails.Tag == "DerivationLogic" || entitiDetails.Tag == "DMUVFunctionality" || entitiDetails.Tag == "Custom")
                    {
                        if(entitiDetails.Tag == "Custom")
                            entitiDetails.Tag = (string)stepContext.Result;
                        else
                            entitiDetails.Tag = ((FoundChoice)stepContext.Result).Value.ToString();
                    }
                    else if (entitiDetails.Tag == "CommitFile")
                    {
                        msg = $"Please confirm, Do you want commit file for   {entitiDetails.MuvKey} ?";
                        return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = MessageFactory.Text(msg) }, cancellationToken);
                    }
                    if (entitiDetails.Tag == null)
                        entitiDetails.Tag = ((FoundChoice)stepContext.Result).Value.ToString();//(string)stepContext.Result;
                    else
                        msg = $"Please confirm, Do you want to execute  {entitiDetails.Tag} for {entitiDetails.Project} ?"; //on {entitiDetails.Environment} {entitiDetails.Client} ?";
                    return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = MessageFactory.Text(msg) }, cancellationToken);
                case "Build_Deployment":
                    if (entitiDetails.Project == "App-Deployment" || entitiDetails.Project == "CIT-Deployment")
                    {
                        if (entitiDetails.Buildwar == null)
                            entitiDetails.Buildwar = (string)stepContext.Result;
                        msg = $"Please confirm, Do you want to Deploy build for the war {entitiDetails.Buildwar} to {entitiDetails.Environment} environment ?";
                        return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = MessageFactory.Text(msg) }, cancellationToken);
                    }
                    else if(entitiDetails.Project == "RMI-Deployment")
                    {
                        entitiDetails.Repo= (string)stepContext.Result;
                        msg = $"Please confirm, Do you want to proceed with RMI deployment on {entitiDetails.Environment} environment for {entitiDetails.Repo} repository?";
                        return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = MessageFactory.Text(msg) }, cancellationToken);
                    }
                    else
                    {
                        if (entitiDetails.DbInstance == null)
                            entitiDetails.DbInstance = (string)stepContext.Result;
                        msg = $"Please confirm, Do you want to proceed with DB deployment for {entitiDetails.Buildwar} on to {entitiDetails.DbInstance} environment?";
                        return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = MessageFactory.Text(msg) }, cancellationToken);
                    }
            }
            await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Sorry I didn't get you.") }, cancellationToken);
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }
        private async Task<DialogTurnResult> CaptureEmailStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {  //var bookingDetails = await LuisHelper.ExecuteLuisQuery(Configuration, Logger, stepContext.Context, cancellationToken);
            var entitiDetails = (EntitiDetails)stepContext.Options;
            if (entitiDetails.Intent == "Build_Deployment")
            {
                if (stepContext.Index > 6 || (bool)stepContext.Result)
                    return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Enter your cotiviti email id to receive deployment status") }, cancellationToken);
                else
                    return await stepContext.EndDialogAsync(null, cancellationToken);
            }
            else if (entitiDetails.Intent == "Acronym")
            {
                if (!string.IsNullOrEmpty(entitiDetails.Acronym))
                {
                    entitiDetails.Acronym = (string)stepContext.Result;
                    return await stepContext.NextAsync(entitiDetails.Acronym, cancellationToken);
                }
                else
                {
                    return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Sorry I didn't get you. Please try other word/Execute a service") }, cancellationToken);
                }
            }
            else if (entitiDetails.Intent == "Trigger_Service")
            {
                if (stepContext.Index > 6 || (bool)stepContext.Result)
                {
                    if (entitiDetails.Tag == entitiDetails.Environment + "AutoDerivation")
                        return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Enter your cotiviti email id to receive auto derivation status") }, cancellationToken);
                    return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Enter your cotiviti email id to receive test results") }, cancellationToken);
                }
                else

                    return await stepContext.EndDialogAsync(null, cancellationToken);
            }
            else
                return await stepContext.EndDialogAsync(null, cancellationToken);
        }
        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var entitiDetails = (EntitiDetails)stepContext.Options;

            if (entitiDetails.Intent == "Trigger_Service" || entitiDetails.Intent == "Build_Deployment")
            {

                entitiDetails.Email = (string)stepContext.Result;
                if (!(entitiDetails.Email.ToLower().Contains("@cotiviti.com")))
                {
                    stepContext.ActiveDialog.State["stepIndex"] = (int)stepContext.ActiveDialog.State["stepIndex"] - 1;
                    return await CaptureEmailStepAsync(stepContext, cancellationToken);
                }
                return await stepContext.EndDialogAsync(entitiDetails, cancellationToken);
            }
            else if (entitiDetails.Intent == "Acronym")
            {
                //entitiDetails.Email = (string)stepContext.Result;
                /*  if (!(entitiDetails.Email.ToLower().Contains("@cotiviti.com")))
                  {
                      stepContext.ActiveDialog.State["stepIndex"] = (int)stepContext.ActiveDialog.State["stepIndex"] - 1;
                      return await CaptureEmailStepAsync(stepContext, cancellationToken);
                  }*/
                return await stepContext.EndDialogAsync(entitiDetails, cancellationToken);

            }
            else
            {
                return await stepContext.EndDialogAsync(entitiDetails, cancellationToken);
            }
        }
        private static bool IsAmbiguous(string timex)
        {
            var timexProperty = new TimexProperty(timex);
            return !timexProperty.Types.Contains(Constants.TimexTypes.Definite);
        }

        private IList<Choice> GetProjectChoices(string strPortFolio)
        {
            switch (strPortFolio.ToUpper())
            {
                case "PCA":
                    var cardOptions = new List<Choice>()
            {
                new Choice() { Value = "CPW", Synonyms = new List<string>() { "CPW" } },
                new Choice() { Value = "InterpretiveUpdate", Synonyms = new List<string>() { "Interpretive Update","IU" } },
                new Choice() { Value = "CPTICDLinks", Synonyms = new List<string>() { "CPTICD Links" } },
                new Choice() { Value = "ClientProfile", Synonyms = new List<string>() { "Client Profile","CP" } },
                new Choice() { Value = "Medicaid", Synonyms = new List<string>() { "Medicaid" } },
                new Choice() { Value = "ImpactAnalysis", Synonyms = new List<string>() { "Impact Analysis" } },
                new Choice() { Value = "ClientInquiry", Synonyms = new List<string>() { "Client Inquiry" } },
                new Choice() { Value = "CTA", Synonyms = new List<string>() { "CTA" } },
                new Choice() { Value = "PresentationManager", Synonyms = new List<string>() { "Presentation Manager" } },
                new Choice() { Value = "ICD-IU", Synonyms = new List<string>() { "ICDIU","icd","icd-iu","icdiu" } },
                 new Choice() { Value = "ClientApps", Synonyms = new List<string>() { "Client apps","client facing apps"} },
                  new Choice() { Value = "RDA", Synonyms = new List<string>() { "RDA Loaders","Loaders"} },
                   new Choice() { Value = "ARD", Synonyms = new List<string>() { "Builder ARD"} },
            };
                    return cardOptions;


                case "RAPID":
                    cardOptions = new List<Choice>()
            {
                new Choice() { Value = "RMS", Synonyms = new List<string>() { "Retrieval Management" } },
                 new Choice() { Value = "Annocoder", Synonyms = new List<string>() { "Annocoder" } },

            };
                    return cardOptions;
                case "CCV":
                    cardOptions = new List<Choice>()
            {
                new Choice() { Value = "Config_Tool", Synonyms = new List<string>() { "Config Tool" } },
                new Choice() { Value = "CCV-CIT", Synonyms = new List<string>() { "CCV CIT", "CCV ISAI" } },

            };
                    return cardOptions;
                case "BUILD_DEPLOYMENT":
                    cardOptions = new List<Choice>()
            {
                new Choice() { Value = "App-Deployment", Synonyms = new List<string>() { "App-Deployment" } },
                 //new Choice() { Value = "DB-Deployment", Synonyms = new List<string>() { "DB Deployment" } },
                 new Choice() { Value = "RMI-Deployment", Synonyms = new List<string>() { "RMI Deployment" } },
            };
                    return cardOptions;
                default:
                    return null;
            }
        }

        private IList<Choice> GetRapidEnvironments(string strType)
        {
            switch (strType)
            {
                case "Environment":
                    var cardOptions = new List<Choice>() {
                    new Choice() { Value = "QA Backend", Synonyms = new List<string>() { "QA Backend" } },
                    new Choice() { Value = "QA Meca", Synonyms = new List<string>() { "QA Meca" } },
                    new Choice() { Value = "UAT", Synonyms = new List<string>() { "UAT" } },
                    new Choice() { Value = "PROD", Synonyms = new List<string>() { "PROD" } },};
                    return cardOptions;
                case "Client":
                    cardOptions = new List<Choice>() {
                    new Choice() { Value = "ROC", Synonyms = new List<string>() { "ROC" } },
                    new Choice() { Value = "HUMANA-CORPORATE", Synonyms = new List<string>() { "HUMANA" } }, };
                    return cardOptions;
                default:
                    return null;
            }
        }

        IList<Choice> GetAnnocoderEnvironments(string strType)
        {
            switch (strType)
            {
                case "Environment":
                    var cardOptions = new List<Choice>() {                    
                    new Choice() { Value = "QA", Synonyms = new List<string>() { "QA" } },
                    new Choice() { Value = "UAT", Synonyms = new List<string>() { "UAT" } },
                    /*new Choice() { Value = "PROD", Synonyms = new List<string>() { "PROD" } },*/};
                    return cardOptions;
                case "Client":
                    cardOptions = new List<Choice>() {
                    //new Choice() { Value = "CHARTNAV", Synonyms = new List<string>() { "CHARTNAV" } },
                    new Choice() { Value = "MECA", Synonyms = new List<string>() { "MECA" } }, 
                    new Choice() { Value = "RMS", Synonyms = new List<string>() { "RMS" } }, };
            return cardOptions;
                default:
                    return null;
            }
        }
    }
}
