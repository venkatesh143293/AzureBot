// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CoreBot.Bots;
using CoreBot.Dialogs;
using CoreBot.Helpers;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Attachment = Microsoft.Bot.Schema.Attachment;
using LotusNotesService;
using Newtonsoft.Json.Linq;
using AdaptiveCards.Templating;


namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class MainDialog : ComponentDialog
    {

        protected readonly IConfiguration Configuration;
        protected readonly ILogger Logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private bool isContinue = false;

        //private IHostingEnvironment _hostingEnvironment;
        private string strRole = string.Empty;
        private string strUser = string.Empty;
        private string strIntent = string.Empty;
        private string strProject = string.Empty;
        private string strJenkinUrl = string.Empty;
        private string guid = string.Empty;

        public MainDialog(IConfiguration configuration, ILogger<MainDialog> logger, IHttpClientFactory httpClientFactory)//
            : base(nameof(MainDialog))
        {
            Configuration = configuration;
            Logger = logger;
            _httpClientFactory = httpClientFactory;

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new OptionPrompt(nameof(OptionPrompt)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new ActionDialog(configuration, logger));
            AddDialog(new LotusNotesDialog(configuration, logger));
            AddDialog(new DeploymentDialog(configuration, logger));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                IntroStepAsync,
                ActStepAsync,
                FinalStepAsync,
                FeedbackStepAsync,

            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }


        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)

        {
            try
            {
                if (string.IsNullOrEmpty(Configuration["LuisAppId"]) || string.IsNullOrEmpty(Configuration["LuisAPIKey"]) || string.IsNullOrEmpty(Configuration["LuisAPIHostName"]))
                {
                    await stepContext.Context.SendActivityAsync(
                        MessageFactory.Text("NOTE: LUIS is not configured. To enable all capabilities, add 'LuisAppId', 'LuisAPIKey' and 'LuisAPIHostName' to the appsettings.json file. "), cancellationToken);

                    return await stepContext.NextAsync(null, cancellationToken);
                }
                else
                {
                    StorageHelper storageHelper = new StorageHelper();
                    strUser = stepContext.Context.Activity.From.Name;
                    string strAaId = stepContext.Context.Activity.From.AadObjectId;
                    //string strAaId = "73d40a33-182b-4daa-b84b-e66d8f9f62b9";
                    string[] userName = new string[2];
                    if (!string.IsNullOrEmpty(strUser))
                        userName = strUser.Split(" ");
                    else
                        userName[0] = "User";
                    strRole = storageHelper.GetRole(Configuration, strAaId, strAaId).Result;
                    if (userName.Length > 1)
                        strUser = userName[1];
                    else
                        strUser = userName[0];
                    if (strRole.ToUpper().Contains("OTHER"))
                    {
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text("Hi " + strUser + "\nSorry!! You dont have access to floraa actions. Please contact administrator."), cancellationToken);
                        return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                    }
                    string strMsg = string.Empty;
                    var entitiDetails = (EntitiDetails)stepContext.Options;                    
                    if (entitiDetails == null || entitiDetails.Returnmsg == "returnQuit")
                        strMsg = "Hi " + strUser + ". Please Type any action below";
                    else if (entitiDetails.Returnmsg == "return")
                        strMsg = "What more you want to know? Please select option";
                    return await stepContext.PromptAsync(nameof(OptionPrompt), new PromptOptions
                    {
                        Prompt = MessageFactory.Text(strMsg),
                        Choices = GetRoleBasedMenu(getActions(strRole, "Action")),
                        Style = ListStyle.Auto,
                        RetryPrompt = MessageFactory.Text("Sorry, I'm still learning. Please provide the valid option or below mentioned Sequence Number."),
                    }, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text(ex.ToString()) }, cancellationToken);

            }
        }

        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            // Call LUIS and gather any potential action details. (Note the TurnContext has the response to the prompt.)
            EntitiDetails eDetails = new EntitiDetails();
            if (stepContext.Result != null)
            {
                string choice = ((FoundChoice)stepContext.Result).Value.ToString();
                switch (choice)
                {
                    case "Test Execution":
                        eDetails.Score = 0.9;
                        eDetails.Intent = "Trigger_Service";
                        break;
                    case "Deploy Build":
                        eDetails.Score = 0.9;
                        eDetails.Intent = "Build_Deployment";
                        break;
                    case "Execute Production Health Check":
                        eDetails.Score = 0.9;
                        eDetails.Intent = "Trigger_Service";
                        eDetails.Project = "ProductionDailyHealthCheck";
                        break;
                    case "Useful Links":
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text("Click on the link: [Useful Links](https://floraafeedback.z13.web.core.windows.net/CotivitiUsefulLink.html)"), cancellationToken);
                        return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                    case "Auto Derivation":
                        eDetails.Score = 0.9;
                        eDetails.Intent = "Trigger_Service";
                        eDetails.Project = "CPAutoDerivation";
                        eDetails.Tag = "AutoDerivation";
                        eDetails.Portfolio = "PCA";
                        break;
                    case "Update Rule in VPMTST1":
                        eDetails.Score = 0.9;
                        eDetails.Intent = "ManageRule";
                        break;
                    case "RuleRelationShip":
                        eDetails.Score = 0.9;
                        eDetails.Intent = "RuleRelationShip";
                        break;
                }

            }
            else
            {
                eDetails = stepContext.Context.Activity.Text != null
                          ?
                      await LuisHelper.ExecuteLuisQuery(Configuration, Logger, stepContext.Context, cancellationToken)
                          :
                      new EntitiDetails();
            }
            StorageHelper storageHelper = new StorageHelper();
            string[] actions = getActions(strRole, "Action");
            if (!actions.Contains(eDetails.Intent))
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Sorry. You dont have access to selected action. Please contact administrator."), cancellationToken);
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            }
            if (eDetails.Intent == "Trigger_Service")
            {
                strJenkinUrl = Configuration["JenkinsURL1"];
                if (Configuration["CheckJenkins"] == "true")
                    strJenkinUrl = CheckJenkinsServer();
                if (string.IsNullOrEmpty(strJenkinUrl) || Configuration["IsJenkinsDown"] == "true")
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(Configuration["MaintainanceMessage"]), cancellationToken);
                    return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                }
            }
            else if (eDetails.Intent == "UsefulLinks")
            {

                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Click on the link: [Useful Links](https://floraafeedback.z13.web.core.windows.net/CotivitiUsefulLink.html)"), cancellationToken);
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            }
            if ((string.IsNullOrEmpty(eDetails.Intent) || eDetails.Score < 0.5))
            {
                var httpClient = _httpClientFactory.CreateClient();

                var qnaMaker = new QnAMaker(new QnAMakerEndpoint
                {
                    KnowledgeBaseId = Configuration["QnAKnowledgebaseId"],
                    EndpointKey = Configuration["QnAEndpointKey"],
                    Host = Configuration["QnAEndpointHostName"]
                },
                null,
                httpClient);
                EntitiDetails entitiDetails1 = new EntitiDetails();
                if (stepContext.Result != null && stepContext.Result.ToString().ToUpper().Trim() == "QUIT")
                {
                    entitiDetails1.Returnmsg = "returnQuit";
                    return await stepContext.BeginDialogAsync(nameof(MainDialog), entitiDetails1, cancellationToken);
                }
                else
                    entitiDetails1.Returnmsg = "return";
                var response = await qnaMaker.GetAnswersAsync(stepContext.Context);
                if (response != null && response.Length > 0)
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(response[0].Answer), cancellationToken);
                    await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                    return await stepContext.BeginDialogAsync(nameof(MainDialog), entitiDetails1, cancellationToken);
                }
                else
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("Sorry. I didn't get you. Please select appropriate action"), cancellationToken);
                    return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                }
            }
            eDetails.Role = strRole;
            if (eDetails.Intent.Equals("ManageRule") || eDetails.Intent.Equals("RuleRelationShip"))
                return await stepContext.BeginDialogAsync(nameof(LotusNotesDialog), eDetails, cancellationToken);
            else if (eDetails.Intent.Equals("Build_Deployment"))
                return await stepContext.BeginDialogAsync(nameof(DeploymentDialog), eDetails, cancellationToken);
            else
                return await stepContext.BeginDialogAsync(nameof(ActionDialog), eDetails, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            try
            {
                if (stepContext.Result != null)
                {
                    StorageHelper storageHelper = new StorageHelper();
                    var entitiDetails = (EntitiDetails)stepContext.Result;
                    strIntent = entitiDetails.Intent;
                    FeedbackEntity feedbackEntity = new FeedbackEntity();
                    feedbackEntity.PartitionKey = stepContext.Context.Activity.From.Name;
                    feedbackEntity.RowKey = guid = Guid.NewGuid().ToString();
                    feedbackEntity.Role = storageHelper.GetRole(Configuration, stepContext.Context.Activity.From.AadObjectId, stepContext.Context.Activity.From.AadObjectId).Result;
                    feedbackEntity.Status = "False";
                    feedbackEntity.FeedBack = string.Empty;
                    feedbackEntity.Intent = strIntent;
                    feedbackEntity.Project = entitiDetails.Project + "-" + entitiDetails.Tag + "-" + entitiDetails.Buildwar;
                    await storageHelper.StoreFeedback(Configuration, feedbackEntity);
                    switch (entitiDetails.Intent)
                    {
                        case "Acronym":
                            return await AcronymsGet(stepContext, entitiDetails, cancellationToken);
                        case "Trigger_Service":
                            return await TriggerServiceMethod(stepContext, entitiDetails, cancellationToken);
                        case "Build_Deployment":
                            return await BuildDeployments(stepContext, entitiDetails, cancellationToken);
                        case "ManageRule":
                            return await LotusNotesUpdates(stepContext, entitiDetails, cancellationToken);
                        case "RuleRelationShip":
                            await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(CreateAdaptiveCardAttachment(entitiDetails.manageRule.Description)), cancellationToken);
                            return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions
                            {
                                Prompt = MessageFactory.Text("Please share your feedback"),
                                Choices = ChoiceFactory.ToChoices(new List<string> { "need improvement", "good", "awesome" }),
                                RetryPrompt = MessageFactory.Text("Sorry, Please seclect any of the option."),
                            }, cancellationToken);
                        default:
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Sorry.. I didn't get you Please Try again.\nThank you"), cancellationToken);
                            isContinue = false;
                            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                    }

                }
                else
                {
                    isContinue = false;
                    var msg = "Thank you " + getUserName(stepContext);
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(msg), cancellationToken);
                    return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Error occured while running service. Please try agian\nThank you " + getUserName(stepContext) + ".\n"), cancellationToken);
                throw;
            }

        }

        private async Task<DialogTurnResult> LotusNotesUpdates(WaterfallStepContext stepContext, EntitiDetails entitiDetails, CancellationToken cancellationToken)
        {
            try
            {
                string sPOSTURL = Configuration["JenkinsURL1"] + "/job/" + entitiDetails.Project + "/buildWithParameters?MidRuleVersion=" + entitiDetails.manageRule.MidruleVersion + "&TagName=@" + entitiDetails.Tag + "&EmailID=" + entitiDetails.Email + "&SrcName=Floraa&LotusUser=" + entitiDetails.manageRule.LotusNotesUser + "&LotusPRMID=" + entitiDetails.manageRule.PresentationId;
                HttpWebRequest requestObjPost = (HttpWebRequest)HttpWebRequest.Create(sPOSTURL);
                requestObjPost.Method = "POST";
                requestObjPost.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes("TestAuto:Ihealth@123"));
                requestObjPost.ContentType = "application/json";
                requestObjPost.UserAgent = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.106 Safari/537.36";
                requestObjPost.Proxy.Credentials = System.Net.CredentialCache.DefaultCredentials;
                using (var streamWriter = new StreamWriter(requestObjPost.GetRequestStream()))
                {
                    var httpResponse = (HttpWebResponse)requestObjPost.GetResponse();
                    if (httpResponse.StatusCode == HttpStatusCode.Created)
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text(entitiDetails.Tag + " is in progress you will receive email with the status shortly"), cancellationToken);
                }
                return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions
                {
                    Prompt = MessageFactory.Text("Please share your feedback"),
                    Choices = ChoiceFactory.ToChoices(new List<string> { "need improvement", "good", "awesome" }),
                    RetryPrompt = MessageFactory.Text("Sorry, Please seclect any of the option."),
                }, cancellationToken);
            }
            catch
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Unable to connect to LotusNotes services. Please contact Floraa Support"), cancellationToken);
                return await stepContext.EndDialogAsync(null, cancellationToken);

                throw;
            }

        }

        private string getPRStstus(int key)
        {
            Dictionary<int, string> dicPRstatus = new Dictionary<int, string>() {
                {1, "New" },
                {2, "In Analysis" },
                {3, "In Progress" },
                {4, "In Testing" },
                {5, "Pending" },
                {6, "Awaiting Approval" },
                {7, "Completed" },{8, "Rejected"}
           };
            return dicPRstatus[key];
        }


        /// <summary>
        /// send email
        /// </summary>
        /// <param name="entitiDetails"></param>
        private void SendEmail(EntitiDetails entitiDetails)
        {
            string initiationMail()
            {
                String sHtml = "<!DOCTYPE html>\r\n" +
                               "<html>\r\n" +
                               "<body>\r\n" +
                               "<p style=\"color:#1F497D;\">\r\n" +
                               "Hi All,\r\n" +
                               "</p>\r\n" +
                               "<p style=\"color:#1F497D;\">\r\n" +
                               "We are planning to initiate the Auto Derivation in " + entitiDetails.Environment + " environment on <b>" + entitiDetails.TravelDate + " EST</b> ,will send out an email after completing the same.</p>\r\n" +
                               "<p style=\"color:#1F497D;\">\r\n" +
                               "Please be informed that, during Auto derivation time don’t do any Manual Derivations or don’t access Deltas.\r\n" +
                               "</p>\r\n" +
                                "<p style=\"color:#1F497D;\">\r\n" +
                               "Thanks for your support,\r\n" +
                               "</p>\r\n" +
                               "<p style=\"color:#1F497D;\">\r\n" +
                               "Regards,\r\n" +
                               "<br>\r\n" +
                               "Floraa.\r\n" +
                               "</p>\r\n" +
                               "</body>\r\n" +
                               "</html>\r\n";
                return sHtml;

            }
            MailMessage mail = new MailMessage();
            SmtpClient SmtpServer = new SmtpClient();
            mail.To.Add("shailaja.nuthi@cotiviti.com");
            mail.From = new MailAddress("AutoDerivation.Floraa@cotiviti.com");
            mail.Subject = "RE: Max Units - " + DateTime.Now.ToString("MMM") + " " + DateTime.Now.Year.ToString() + " - Initiation of Auto Derivation in " + entitiDetails.Environment;
            mail.IsBodyHtml = true;
            mail.Body = initiationMail();
            SmtpServer.Host = "ihtmail.ihealthtechnologies.com";
            SmtpServer.Port = 25;
            SmtpServer.DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network;
            SmtpServer.Send(mail);
        }

        private async Task<DialogTurnResult> FeedbackStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            StorageHelper storageHelper = new StorageHelper();
            FeedbackEntity feedbackEntity = new FeedbackEntity();
            feedbackEntity.PartitionKey = stepContext.Context.Activity.From.Name;
            feedbackEntity.RowKey = guid;
            feedbackEntity.Role = storageHelper.GetRole(Configuration, stepContext.Context.Activity.From.AadObjectId, stepContext.Context.Activity.From.AadObjectId).Result;
            feedbackEntity.Status = "true";
            feedbackEntity.FeedBack = ((FoundChoice)stepContext.Result).Value.ToString();
            feedbackEntity.Intent = strIntent;

            await storageHelper.StoreFeedback(Configuration, feedbackEntity);
            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Thank you for your feedback"), cancellationToken);
            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }
        private async Task<DialogTurnResult> TriggerServiceMethod(WaterfallStepContext stepContext, EntitiDetails entitiDetails, CancellationToken cancellationToken)
        {
            string strTag = "";
            if (entitiDetails.Tag.ToUpper() == "COMMITFILE")
            {
                string msg = "";
                string URL = "https://floraa-acronymapi.azurewebsites.net/api/values/GetCommitFile?muvKey=" + entitiDetails.MuvKey + "&mailID=" + entitiDetails.Email;
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(URL);
                req.Method = "GET";
                req.Headers.Add("X-ApiKey", "6b0f60c2-40ef-43d5-89ef-905e048d610b:a99b5fcb-064f-433a-afde-e0e46f441005");
                req.Accept = "text/json";
                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                {

                    if (resp.StatusCode != HttpStatusCode.OK)
                    {
                        msg = $"Commit file for " + entitiDetails.MuvKey + " will be sent to your mail shortly";
                    }
                    else
                        msg = $"Commit file for " + entitiDetails.MuvKey + " not found please try other key";
                }
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(msg), cancellationToken);
                //return await feedBackPromt(stepContext);
                return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions
                {
                    Prompt = MessageFactory.Text("Please share your feedback"),
                    Choices = ChoiceFactory.ToChoices(new List<string> { "need improvement", "good", "awesome" }),
                    RetryPrompt = MessageFactory.Text("Sorry, Please seclect any of the option."),
                }, cancellationToken);
            }
            else if (entitiDetails.Tag.ToUpper() == "SMOKE" && entitiDetails.Portfolio != "Rapid")
            {
                strTag = entitiDetails.Tag;
                entitiDetails.Tag = "QASmoke";
            }
            else if (entitiDetails.Tag.ToUpper() == "REGRESSION" && entitiDetails.Project != "CCV-CIT" && entitiDetails.Portfolio != "Rapid")
            {
                strTag = "Regression";
                entitiDetails.Tag = "Sanity";

                JenkinsService jenkinsService = new JenkinsService();
                var lastBuild = jenkinsService.getLastBuildStatus(entitiDetails.Project).Result;
                var lastBuildType = lastBuild["actions"];

                if (lastBuild != null)
                {
                    var x = lastBuildType[0]["parameters"][0];
                    var sTagType = lastBuildType[0]["parameters"][0]["value"];
                    //   var a1 = lastBuildType["_class"]["parameter"];

                    var lastBuildStatus = lastBuild["result"].ToString();
                    if (string.IsNullOrEmpty(lastBuildStatus) || lastBuildStatus == "{}")
                    {
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text("Same user or some other user has already triggerd this project test execution please try after some time."), cancellationToken);
                        return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                    }
                    else if (sTagType == "@Sanity" || sTagType == "@Regression")
                    {
                        var lastBuildTime = (long)lastBuild["timestamp"];

                        var timeStamp = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds;

                        var timeDiference = (timeStamp - lastBuildTime);

                        var timeMinutes = timeDiference / (1000 * 60);
                        if (timeMinutes <= 1200)
                        {
                            var url = lastBuild["url"].ToString() + "Serenity_20Report/";
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text("This is already executed in last one hour click on the link to view results: [Regression Results](" + url + ")"), cancellationToken);
                            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);

                        }
                    }
                }
            }
            else if (entitiDetails.Tag.ToUpper() == "REGRESSION" && entitiDetails.Project == "CCV-CIT")
            {
                entitiDetails.Tag = "Sanity";
            }
            string sPOSTURL = "";
            strProject = entitiDetails.Project + "-" + entitiDetails.Tag;
            if (entitiDetails.Tag.ToUpper().Contains("AUTODERIVATION"))
            {
                HttpWebRequest requestObjGet = (HttpWebRequest)HttpWebRequest.Create(Configuration["AutoDerivationUrl"].Replace("qa", entitiDetails.Environment.ToLower()));
                requestObjGet.Method = "GET";
                var httpResponse = (HttpWebResponse)requestObjGet.GetResponse();
                if (httpResponse.StatusCode == HttpStatusCode.OK)
                {
                    if (entitiDetails.ScheduledOption == "Schedule")
                    {
                        SendEmail(entitiDetails);
                        sPOSTURL = strJenkinUrl + "/job/" + entitiDetails.Project + "/buildWithParameters?TagName=@" + entitiDetails.Tag + "&EmailID=" + entitiDetails.Email + "&SrcName=Floraa&delay=" + entitiDetails.JenkinsDelay + "sec";
                    }
                    else
                        sPOSTURL = strJenkinUrl + "/job/" + entitiDetails.Project + "/buildWithParameters?TagName=@" + entitiDetails.Tag + "&EmailID=" + entitiDetails.Email + "&SrcName=Floraa";
                }
                else
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("Sorry something went wrong,Please try again after sometime."), cancellationToken);
                    return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
                }
            }
            else if (entitiDetails.Portfolio == "Rapid" && entitiDetails.Project.ToUpper() != "RETRIEVALMANAGEMENT")
            {
                if (entitiDetails.Project == "Annocoder")
                {
                    var env = (entitiDetails.Environment + "_" + entitiDetails.Client).Replace("-", "_").ToLower();
                    entitiDetails.Tag = entitiDetails.Tag + " and @" + entitiDetails.Environment + " and @" + entitiDetails.Client;
                    sPOSTURL = Configuration["JenkinsURL2"] + "/job/Annocoder/buildWithParameters?TagName=@" + entitiDetails.Tag + "&EmailID=" + entitiDetails.Email + "&SrcName=Floraa&Environment=" + env;
                }
                else
                {
                    var env = (entitiDetails.Environment == "PROD") ? entitiDetails.Environment.ToLower() : ((entitiDetails.Environment + "_" + entitiDetails.Client).Replace("QA Backend", "QA_DIRECT").Replace("QA Meca", "QA").Replace("-", "_")).ToLower();
                    entitiDetails.Tag = (entitiDetails.Environment == "PROD") ? entitiDetails.Tag + " and @" + entitiDetails.Environment : entitiDetails.Tag + " and @" + entitiDetails.Environment + " and @" + entitiDetails.Client;
                    sPOSTURL = Configuration["JenkinsURL2"] + "/job/RMS_Execution/buildWithParameters?TagName=@" + entitiDetails.Tag.Replace("QA Backend", "QA_DIRECT").Replace("QA Meca", "QA") + "&EmailID=" + entitiDetails.Email + "&SrcName=Floraa&Environment=" + env;
                }

            }
            else
            {
                entitiDetails.Environment = string.IsNullOrEmpty(entitiDetails.Environment) ? string.Empty : entitiDetails.Environment.ToLower();
                if (entitiDetails.Tag.StartsWith("@"))
                {
                    entitiDetails.Tag = entitiDetails.Tag.Substring(1);
                }
                sPOSTURL = strJenkinUrl + "/job/" + entitiDetails.Project + "/buildWithParameters?Profile=" + Configuration["Profile"] + "&TagName=@" + entitiDetails.Tag + "&EmailID=" + entitiDetails.Email + "&SrcName=Floraa&Environment=" + entitiDetails.Environment;
            }
            HttpWebRequest requestObjPost = (HttpWebRequest)HttpWebRequest.Create(sPOSTURL);
            requestObjPost.Method = "POST";
            if (Convert.ToBoolean(Configuration["IsCrumbRequired"]))
                requestObjPost.Headers["Jenkins-Crumb"] = generateJenkinsCrumb(strJenkinUrl);
            requestObjPost.Headers["Authorization"] = Configuration["JenkinsAuthKey"];
            requestObjPost.ContentType = "application/json";
            requestObjPost.UserAgent = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.106 Safari/537.36";
            requestObjPost.Proxy.Credentials = System.Net.CredentialCache.DefaultCredentials;
            using (var streamWriter = new StreamWriter(requestObjPost.GetRequestStream()))
            {
                var httpResponse = (HttpWebResponse)requestObjPost.GetResponse();
                if (httpResponse.StatusCode == HttpStatusCode.Created)
                {
                    if (entitiDetails.Tag.ToUpper() == "AUTODERIVATION")
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text("Triggered " + entitiDetails.Tag + " for " + entitiDetails.Project + ". The results will be sent to your email shortly."), cancellationToken);
                    else

                        await stepContext.Context.SendActivityAsync(MessageFactory.Text("Triggered " + strTag + " for " + entitiDetails.Project + ". The results will be sent to your email shortly. \n You can also see the live execution in below URL: [Click Here](http://usddccntr04:8080/) "), cancellationToken);

                    if (entitiDetails.Tag == "Sanity")
                    {

                        await stepContext.Context.SendActivityAsync(MessageFactory.Text("Estimated time to complete the execution 30-45 mins"), cancellationToken);
                    }

                }
            }

            isContinue = false;
            //return await feedBackPromt(stepContext);
            return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions
            {
                Prompt = MessageFactory.Text("Please share your feedback"),
                Choices = ChoiceFactory.ToChoices(new List<string> { "need improvement", "good", "awesome" }),
                RetryPrompt = MessageFactory.Text("Sorry, Please seclect any of the option."),
            }, cancellationToken);
        }
        private async Task<DialogTurnResult> BuildDeployments(WaterfallStepContext stepContext, EntitiDetails entitiDetails, CancellationToken cancellationToken)
        {
            try
            {
                string msg = string.Empty;
                strProject = entitiDetails.Project;
                string buildURL = string.Empty;
                if (strProject == "App-Deployment")
                    buildURL = Configuration["JenkinsBuildDeploymentURL"] + "/jenkins/job/floraa_qadeployer/buildWithParameters?token=floradeploy&Floraa_Intent=" + entitiDetails.Buildwar + "&Email=Support.floraa@cotiviti.com," + entitiDetails.Email + "&Environment=" + entitiDetails.Environment.ToLower() + "&DeployedThru=Floraa&Force=" + entitiDetails.isForceDeployment.ToString().ToLower();
                else if (strProject == "DB-Deployment")
                    if (entitiDetails.ScriptName.Equals("PM.SQL"))
                        buildURL = Configuration["JenkinsBuildDeploymentURL"] + "/jenkins/job/" + entitiDetails.DBDeploymenttype + "/buildWithParameters?token=floradbdeploy&Script_Name=" + entitiDetails.ScriptName + "&DBInstance=" + entitiDetails.DbInstance + "&Upgrades_FolderName=" + entitiDetails.Buildwar + "&EmailRecipients=" + entitiDetails.Email + "&Repository_Name=" + entitiDetails.Repo + "&DB_RESTRICT_MODE=" + entitiDetails.isDBRestricted + "&CODE_CUTOFF=" + entitiDetails.codeCutOff + "&DeployedThru=Floraa";
                    else
                        buildURL = Configuration["JenkinsBuildDeploymentURL"] + "/jenkins/job/" + entitiDetails.DBDeploymenttype + "/buildWithParameters?token=floradbdeploy&Script_Name=" + entitiDetails.ScriptName + "&DBInstance=" + entitiDetails.DbInstance + "&Upgrades_FolderName=" + entitiDetails.Buildwar + "&EmailRecipients=" + entitiDetails.Email + "&Repository_Name=" + entitiDetails.Repo + "&DeployedThru=Floraa";
                else if (strProject == "Informatica-Deployment")
                    buildURL = Configuration["ETLDeploymentURL"] + "/jenkins/job/INFORMATICA_DEPLOYMENT/buildWithParameters?token=floradeploy&ENV=" + entitiDetails.Environment.ToUpper() + "&BRANCH=" + entitiDetails.Repo + "&VERSIONS=" + entitiDetails.Buildversion + "&ZipFile=" + entitiDetails.File.Trim() + "&EmailRecipients=" + entitiDetails.Email + "&DeployedThru=Floraa";
                else if (strProject == "CIT-Deployment")
                    buildURL = Configuration["JenkinsBuildDeploymentURL"] + "/jenkins/job/PCA_CIT-WIT_CI_JOBS/job/floraa_CIT-WIT_deployer/buildWithParameters?token=floradeploy&Floraa_Intent=" + entitiDetails.Buildwar + "&Email=Support.floraa@cotiviti.com," + entitiDetails.Email + "&Environment=" + entitiDetails.Environment.ToLower() + "&DeployedThru=Floraa";
                else
                    buildURL = Configuration["JenkinsBuildDeploymentURL"] + "/jenkins/job/" + Configuration["RMIDeploymentJob"] + "/buildWithParameters?token=rmifloraadeploy&DBINST=" + entitiDetails.DbInstance + "&HOSTNAME=" + entitiDetails.HostName + "&EmailRecipients=" + entitiDetails.Email + "&BB_REPO=" + entitiDetails.Repo.ToLower() + "&DeployedThru=Floraa";
                HttpWebRequest reqObj = (HttpWebRequest)HttpWebRequest.Create(buildURL);
                reqObj.Method = "POST";
                //reqObj.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes("TestAuto:Ihealth@123"));
                reqObj.ContentType = "application/json";
                reqObj.UserAgent = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.106 Safari/537.36";
                reqObj.Proxy.Credentials = System.Net.CredentialCache.DefaultCredentials;
                using (var streamWriter = new StreamWriter(reqObj.GetRequestStream()))
                {
                    var httpResponse = (HttpWebResponse)reqObj.GetResponse();
                    if (httpResponse.StatusCode == HttpStatusCode.Created)
                    {
                        if (strProject == "App-Deployment")
                            msg = "App Deployment with war " + entitiDetails.Buildwar + " to " + entitiDetails.Environment + " is initiated. you will receive the email shortly.";
                        else if (strProject == "ETL - Deployment")
                        {
                            if (!string.IsNullOrEmpty(entitiDetails.File))
                                msg = entitiDetails.Project + " is initiated. Estimated time to complete the deployment is 5 minutes.You will receive the email once the deployment is completed.";
                            else
                                msg = entitiDetails.Project + " is initiated. Estimated time to complete the deployment is 45 minutes.You will receive the email once the deployment is completed.";
                        }
                        else
                            msg = entitiDetails.Project + " is initiated. you will receive the email shortly.";
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text(msg), cancellationToken);
                    }
                }
                isContinue = false;
                //return await feedBackPromt(stepContext);
                return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions
                {
                    Prompt = MessageFactory.Text("Please share your feedback"),
                    Choices = ChoiceFactory.ToChoices(new List<string> { "need improvement", "good", "awesome" }),
                    RetryPrompt = MessageFactory.Text("Sorry, Please seclect any of the option."),
                }, cancellationToken);
            }
            catch (Exception ex)
            {

                throw;
            }

        }

        private async Task<DialogTurnResult> AcronymsGet(WaterfallStepContext stepContext, EntitiDetails entitiDetails, CancellationToken cancellationToken)
        {
            // If the child dialog ("ActionDialog") was cancelled or the user failed to confirm, the Result here will be null.                        
            var result = (EntitiDetails)stepContext.Result;
            var msg = string.Empty;
            // Now we have all the Action details call the action service.
            string URL = "https://floraa-acronymapi.azurewebsites.net/api/values/GetAcronym?id=" + result.Acronym;
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(URL);
            req.Method = "GET";
            req.Headers.Add("X-ApiKey", "6b0f60c2-40ef-43d5-89ef-905e048d610b:a99b5fcb-064f-433a-afde-e0e46f441005");
            req.Accept = "text/json";
            using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
            {
                if (resp.StatusCode == HttpStatusCode.OK)
                {
                    StreamReader reader = new StreamReader(resp.GetResponseStream());
                    string strResult = reader.ReadToEnd();
                    if (strResult.Length > 2)
                        msg = strResult;
                    else
                        msg = $"The Acronym of {result.Acronym} not found please try other word";
                }
                else
                    msg = $"The Acronym of {result.Acronym} not found please try other word";
            }
            await stepContext.Context.SendActivityAsync(MessageFactory.Text(msg), cancellationToken);
            //return await feedBackPromt(stepContext);
            return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions
            {
                Prompt = MessageFactory.Text("Please share your feedback"),
                Choices = ChoiceFactory.ToChoices(new List<string> { "need improvement", "good", "awesome" }),
                RetryPrompt = MessageFactory.Text("Sorry, Please seclect any of the option."),
            }, cancellationToken);
        }

        private string getUserName(WaterfallStepContext stepContext)
        {
            string user = stepContext.Context.Activity.From.Name;
            string[] userName = new string[2];
            if (!string.IsNullOrEmpty(user))
                userName = user.Split(" ");
            else
                userName[0] = "User";
            if (userName.Length > 1)
                user = userName[1];
            else return "User";
            return user;


        }

        private string CheckJenkinsServer()
        {
            HttpWebRequest request = WebRequest.Create(Configuration["JenkinsURL1"]) as HttpWebRequest;
            //Setting the Request method HEAD, you can also use GET too.
            request.Method = "HEAD";
            request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes("TestAuto:Ihealth@123"));
            request.ContentType = "application/json";
            request.UserAgent = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.106 Safari/537.36";
            request.Proxy.Credentials = CredentialCache.DefaultCredentials;
            //Getting the Web Response.
            try
            {
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                return Configuration["JenkinsURL1"];
            }
            catch (Exception)
            {
                try
                {
                    request = WebRequest.Create(Configuration["JenkinsURL2"]) as HttpWebRequest;
                    request.Method = "HEAD";
                    request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes("TestAuto:Ihealth@123"));
                    request.ContentType = "application/json";
                    request.UserAgent = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.106 Safari/537.36";
                    request.Proxy.Credentials = CredentialCache.DefaultCredentials;
                    HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                    return Configuration["JenkinsURL2"];
                }
                catch (Exception)
                {

                    return string.Empty;
                }
            }


        }

        // Load attachment from file.
        private Attachment CreateAdaptiveCardAttachment(string rule)
        {
            // combine path for cross platform support
            string[] paths = { ".", "Cards", "RuleTemplate.json" };
            string fullPath = Path.Combine(paths);
            string adaptiveCard = File.ReadAllText(fullPath);
            AdaptiveCardTemplate template = new AdaptiveCardTemplate(adaptiveCard);
            var mydata = getRuleRelationshipDetails(rule);
            string cardjson = template.Expand(mydata);
            return new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(cardjson),
            };
        }

        private IList<Choice> GetRoleBasedMenu(string[] actions)
        {
            var cardOptions = new List<Choice>();
            foreach (string action in actions)
            {
                if (action.Equals("Trigger_Service"))
                {
                    cardOptions.Add(new Choice() { Value = "Test Execution" });
                    cardOptions.Add(new Choice() { Value = "Execute Production Health Check" });

                }
                if (action.Equals("Build_Deployment"))
                    cardOptions.Add(new Choice() { Value = "Deploy Build" });
                if (action.Equals("UsefulLinks"))
                    cardOptions.Add(new Choice() { Value = "Useful Links" });
                if (action.Equals("AutoDerivation"))
                    cardOptions.Add(new Choice() { Value = "Auto Derivation" });
                if (action.Equals("ManageRule"))
                    cardOptions.Add(new Choice() { Value = "Update Rule in VPMTST1" });
                if (action.Equals("RuleRelationShip"))
                    cardOptions.Add(new Choice() { Value = "RuleRelationShip" });
            }
            return cardOptions;
        }

        public string[] getActions(string strRole, string action)
        {
            StorageHelper storageHelper = new StorageHelper();
            StringBuilder actionBuilder = new StringBuilder();
            string[] roles = strRole.Split(',');
            foreach (string role in roles)
                actionBuilder.Append(storageHelper.getActions(Configuration, role, action).Result).Append(',');
            return actionBuilder.ToString().Trim(',').Split(',').Distinct().ToArray();
        }

        private async Task<string> createPRrequest(ManageRule manageRule)
        {
            manageRule.PresentationId = Configuration["PRESENTATIONID"];
            manageRule.CDMUrl = Configuration["CDMURL"];
            manageRule.Category = Configuration["CATEGORY"];
            manageRule.CategoryName = Configuration["CREATORNAME"];
            manageRule.RequestDate = DateTime.Now.ToShortDateString();
            manageRule.DCD = DateTime.Now.ToShortDateString();
            manageRule.Payers = Configuration["PAYERS"];
            manageRule.Summary = Configuration["SUMMARY"];
            manageRule.Description = Configuration["DESCRIPTION"];
            LotusNotesService.NotesWebServicesCDMClient _client = new NotesWebServicesCDMClient();
            LotusNotesService.NOTESRETURN _response = new NOTESRETURN();
            _response = (NOTESRETURN)await _client.CREATECDMPROJECTREQUESTAsync(manageRule.LotusNotesUser, manageRule.PresentationId, manageRule.CDMUrl, manageRule.Category, manageRule.Category, manageRule.RequestDate, manageRule.DCD, manageRule.Payers, manageRule.Summary, manageRule.Description);
            return _response.REQID;


        }

        private async Task<string> GETPRSTATUSAsync(string SID)
        {
            LotusNotesService.NotesWebServicesCDMClient _client = new NotesWebServicesCDMClient();
            LotusNotesService.PRDETAILS pRDETAILS = new PRDETAILS();
            pRDETAILS = await _client.GETPRSTATUSAsync(SID);
            return pRDETAILS.PRSTATUS;
        }

        private HttpClient getHttpClient(string url, bool isAuthreq)
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri(url);
            if (isAuthreq)
                client.DefaultRequestHeaders.Add("Authorization", Configuration["JenkinsAuthKey"]);
            return client;

        }
        private string generateJenkinsCrumb(string JenkinsUrl)
        {
            var url = JenkinsUrl + "/crumbIssuer/api/json";
            var client = getHttpClient(JenkinsUrl, true);
            HttpResponseMessage response = client.GetAsync(url).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
            var res = (JObject)JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
            return res["crumb"].ToString();

        }

        private string getRuleRelationshipDetails(string Rule)
        {
            var url = Configuration["RuleRelationBaseURL"] + "/ruledesc1?rule=" + Rule;
            var client = getHttpClient(Configuration["RuleRelationBaseURL"], false);
            HttpResponseMessage response = client.GetAsync(url).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
            var res = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            return res.ToString();
        }
    }
}