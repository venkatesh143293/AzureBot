//using AdaptiveCards;
using System.Web.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.BotBuilderSamples;
using Microsoft.BotBuilderSamples.Dialogs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Attachment = Microsoft.Bot.Schema.Attachment;
using CoreBot.Bots;
using CoreBot.Helpers;

namespace CoreBot.Dialogs
{
    public class DeploymentDialog : CancelAndHelpDialog
    {
        protected readonly IConfiguration Configuration;
        protected readonly ILogger Logger;
        public DeploymentDialog(IConfiguration configuration, ILogger<MainDialog> logger)
            : base(nameof(DeploymentDialog))
        {
            Configuration = configuration;
            Logger = logger;
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new OptionPrompt(nameof(OptionPrompt)));
            AddDialog(new DateResolverDialog());
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                DestinationStepAsync,
                PortfolioStepAsync,
                DBdeploymentStepAsync,
                EnvironmentStepAsync,
                DbInstanceStepAsync,
                VersionStepAsync,
                FileStepAsync,
                DBRestrictedAsync,
                ConfirmStepAsync,
                CaptureEmailStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> DestinationStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(nameof(ChoicePrompt),
             new PromptOptions
             {
                 Prompt = MessageFactory.Text("Please enter the portfolio"),
                 Choices = ChoiceFactory.ToChoices(new List<string> { "PCA", "CCV", "Rapid" }),
                 RetryPrompt = MessageFactory.Text("Sorry, I'm still learning. Please provide the valid option or below mentioned Sequence Number."),
             }, cancellationToken);
        }
        private async Task<DialogTurnResult> PortfolioStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var entitiDetails = (EntitiDetails)stepContext.Options;
            if (!string.IsNullOrEmpty(entitiDetails.Project))
                return await stepContext.NextAsync(entitiDetails, cancellationToken);
            entitiDetails.Portfolio = entitiDetails.Portfolio = ((FoundChoice)stepContext.Result).Value.ToString();
            if (entitiDetails.Portfolio.Equals("PCA"))
                return await stepContext.PromptAsync(nameof(ChoicePrompt),
          new PromptOptions
          {
              Prompt = MessageFactory.Text("Please select the project"),
              Choices = GetProjectChoices(entitiDetails.Intent, entitiDetails.Role),
              Style = ListStyle.Auto,
              RetryPrompt = MessageFactory.Text("Sorry, I'm still learning. Please provide the valid option or below mentioned Sequence Number."),
          }, cancellationToken);
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("No deployments are enabled for selected portfolio. please try with other option "), cancellationToken);
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }

        }
        private async Task<DialogTurnResult> DBdeploymentStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var entitiDetails = (EntitiDetails)stepContext.Options;
            if (!string.IsNullOrEmpty(entitiDetails.Project) && !string.IsNullOrEmpty(entitiDetails.Environment))
                return await stepContext.NextAsync(entitiDetails, cancellationToken);
            else if (string.IsNullOrEmpty(entitiDetails.Project))
                entitiDetails.Project = ((FoundChoice)stepContext.Result).Value.ToString();
            if (entitiDetails.Project == "DB-Deployment")
                return await stepContext.PromptAsync(nameof(ChoicePrompt),
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Please select DB-Deployment type"),
                    Choices = GetProjectChoices(entitiDetails.Project, entitiDetails.Role),
                    RetryPrompt = MessageFactory.Text("Sorry, I'm still learning. Please provide the valid option or below mentioned Sequence Number."),
                }, cancellationToken);
            else
                return await stepContext.NextAsync(entitiDetails, cancellationToken);

        }
        private async Task<DialogTurnResult> EnvironmentStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var entitiDetails = (EntitiDetails)stepContext.Options;
            if (!string.IsNullOrEmpty(entitiDetails.Project) && !string.IsNullOrEmpty(entitiDetails.Environment))
                return await stepContext.NextAsync(entitiDetails, cancellationToken);
            else if (entitiDetails.Project == "DB-Deployment")
            {
                entitiDetails.ScriptName = ((FoundChoice)stepContext.Result).Value.ToString();
                Dictionary<string, string> dic = new Dictionary<string, string>();
                dic.Add("PM.SQL", "PCA_Sql_Runner");
                dic.Add("API.SQL", "API_PCA_Sql_Runner");
                dic.Add("ICMSDB.SQL", "ICMS_DB_Deployer");
                dic.Add("Others", "ICMS_DB_Deployer");
                entitiDetails.DBDeploymenttype = dic[entitiDetails.ScriptName];
            }
            return await stepContext.PromptAsync(nameof(ChoicePrompt),
            new PromptOptions
            {
                Prompt = MessageFactory.Text("Please select the environment"),
                Choices = GetEnvironments(entitiDetails.Project),
                RetryPrompt = MessageFactory.Text("Sorry, I'm still learning. Please provide the valid option or below mentioned Sequence Number."),
            }, cancellationToken);

        }


        private async Task<DialogTurnResult> DbInstanceStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var entitiDetails = (EntitiDetails)stepContext.Options;
            if (entitiDetails.Environment == null)
                entitiDetails.Environment = ((FoundChoice)stepContext.Result).Value.ToString();
            if (entitiDetails.Project == "App-Deployment")
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please enter war to deploy the build.\n\n" + " Ex:Ipp-Portal:<version>,Loginservice:<version>,Client-Profile:<version>") }, cancellationToken);
            else if (entitiDetails.Project == "CIT-Deployment")
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please enter war to deploy the build.\n\n" + " Ex: hello-world.war:<version>") }, cancellationToken);

            else if (entitiDetails.Project == "RMI-Deployment" || entitiDetails.Project == "DB-Deployment")
            {
                if (entitiDetails.Project == "RMI-Deployment")
                    entitiDetails.HostName = entitiDetails.Environment == "QA(VPMTST1)" ? "usdtrmi03" : "usddevrmi01";
                Dictionary<string, string> dic = new Dictionary<string, string>();
                dic.Add("QA(VPMTST1)", "VPMTST1");
                dic.Add("SprintTest(VPMSPTE)", "VPMSPTE");
                dic.Add("SprintDemo(VPMDEMO)", "VPMDEMO");
                dic.Add("CICD(VPMCICD)", "VPMCICD");
                entitiDetails.DbInstance = dic[entitiDetails.Environment].ToString();
                if (entitiDetails.Project == "DB-Deployment")
                    return await stepContext.PromptAsync(nameof(ChoicePrompt),
          new PromptOptions
          {
              Prompt = MessageFactory.Text("Please select from where you wanto to deploy?"),
              Choices = ChoiceFactory.ToChoices(new List<string> { "trunk", "tags" }),
              Style = ListStyle.Auto,
              RetryPrompt = MessageFactory.Text("Sorry, I'm still learning. Please provide the valid option or below mentioned Sequence Number."),
          }, cancellationToken);
                else
                    return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please enter the repository for deployment") }, cancellationToken);
            }
            else if (entitiDetails.Project == "Informatica-Deployment")
                return await stepContext.PromptAsync(nameof(ChoicePrompt),
          new PromptOptions
          {
              Prompt = MessageFactory.Text("Please select the repository branch or press 1 to skip the branch selection"),
              Choices = GetBranches(),
              Style = ListStyle.Auto,
              RetryPrompt = MessageFactory.Text("Sorry, I'm still learning. Please provide the valid option or below mentioned Sequence Number."),
          }, cancellationToken);
            else
            {
                if (entitiDetails.Buildwar == null)
                    entitiDetails.Buildwar = (string)stepContext.Result;
                return await stepContext.NextAsync(entitiDetails, cancellationToken);
            }
        }
        private async Task<DialogTurnResult> VersionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var entitiDetails = (EntitiDetails)stepContext.Options;
            switch (entitiDetails.Project)
            {
                case "Informatica-Deployment":
                    entitiDetails.Repo = ((FoundChoice)stepContext.Result).Value.ToString();
                    entitiDetails.Repo = entitiDetails.Repo.Equals("Skip") ? string.Empty : entitiDetails.Repo;
                    return await stepContext.PromptAsync(nameof(ChoicePrompt),
              new PromptOptions
              {
                  Prompt = MessageFactory.Text("Please select the version of a branch need to deploy or press 1 to skip the version selection"),
                  Choices = GetNexusVersions(),
                  Style = ListStyle.Auto,
                  RetryPrompt = MessageFactory.Text("Sorry, I'm still learning. Please provide the valid option or below mentioned Sequence Number."),
              }, cancellationToken);
                //break;
                case "App-Deployment":
                    entitiDetails.Buildwar = (string)stepContext.Result;
                    var msg = $"Please confirm, Do you want to Deploy build for the war {entitiDetails.Buildwar} to {entitiDetails.Environment} environment ?";
                    return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = MessageFactory.Text("Do you want to do a force deployment?") }, cancellationToken);
                case "DB-Deployment":
                    entitiDetails.Repo = ((FoundChoice)stepContext.Result).Value.ToString();
                    return await stepContext.PromptAsync(nameof(ChoicePrompt),
                    new PromptOptions
                    {
                        Prompt = MessageFactory.Text("Please select the repository name deployment"),
                        Choices = ChoiceFactory.ToChoices(OracleHelper.getOracleDBBranches()),
                        Style = ListStyle.Auto,
                        RetryPrompt = MessageFactory.Text("Sorry, I'm still learning. Please provide the valid option or below mentioned Sequence Number."),
                    }, cancellationToken);
                case "CIT-Deployment":
                    entitiDetails.Buildwar = (string)stepContext.Result;
                    return await stepContext.NextAsync(entitiDetails, cancellationToken);
                case "RMI-Deployment":
                    entitiDetails.Repo = (string)stepContext.Result;
                    return await stepContext.NextAsync(entitiDetails, cancellationToken);
                default:
                    return await stepContext.NextAsync(entitiDetails, cancellationToken);

            }
        }
        private async Task<DialogTurnResult> FileStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var entitiDetails = (EntitiDetails)stepContext.Options;
            if (entitiDetails.Project == "Informatica-Deployment")
            {
                entitiDetails.Buildversion = ((FoundChoice)stepContext.Result).Value.ToString();
                entitiDetails.Buildversion = entitiDetails.Buildversion.Equals("Skip") ? string.Empty : entitiDetails.Buildversion;
                if (!string.IsNullOrEmpty(entitiDetails.Repo))
                    return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please enter file name to deploy the build. Type N/A if not appicable\n\n" + " Ex:LV_NCD_NON_LAB.zip") }, cancellationToken);
                else
                    return await stepContext.NextAsync(entitiDetails, cancellationToken);
            }
            else if (entitiDetails.Project == "App-Deployment")
            {
                entitiDetails.isForceDeployment = (bool)stepContext.Result;
                return await stepContext.NextAsync(entitiDetails, cancellationToken);
            }
            else if (entitiDetails.Project == "DB-Deployment")
            {
                if(!entitiDetails.Repo.Equals("trunk"))
                    entitiDetails.Repo= ((FoundChoice)stepContext.Result).Value.ToString();
                entitiDetails.Buildwar = ((FoundChoice)stepContext.Result).Value.ToString();
                if (entitiDetails.ScriptName.Equals("PM.SQL"))
                    return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please confirm, Do you want to do code cutoff?") }, cancellationToken);
                else
                    return await stepContext.NextAsync(entitiDetails, cancellationToken);
            }
            else
                return await stepContext.NextAsync(entitiDetails, cancellationToken);
        }

        private async Task<DialogTurnResult> DBRestrictedAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var entitiDetails = (EntitiDetails)stepContext.Options;
            if (entitiDetails.Project == "DB-Deployment")
            {
                if (entitiDetails.ScriptName.Equals("PM.SQL"))
                {
                    entitiDetails.codeCutOff = (bool)stepContext.Result;
                    return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please confirm, Do you want to proceed DB deployment in restricted mode?") }, cancellationToken);
                }
                else return await stepContext.NextAsync(entitiDetails, cancellationToken);
            }
            else if (entitiDetails.Project == "Informatica-Deployment")
            {
                if (string.IsNullOrEmpty(entitiDetails.File))
                    entitiDetails.File = stepContext.Result.ToString().Trim().ToUpper().Equals("N/A") || stepContext.Result.ToString().Trim().ToUpper().Equals("NA") ? string.Empty : stepContext.Result.ToString();
                return await stepContext.NextAsync(entitiDetails, cancellationToken);
            }
            else
                return await stepContext.NextAsync(entitiDetails, cancellationToken);
        }

        private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var entitiDetails = (EntitiDetails)stepContext.Options;
            string msg = string.Empty;
            switch (entitiDetails.Project)
            {
                case "Informatica-Deployment":
                    msg = $"Please confirm, Do you want to proceed with ETL deployment on {entitiDetails.Environment} environment for {entitiDetails.Repo} repository?";
                    return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = MessageFactory.Text(msg) }, cancellationToken);
                case "App-Deployment":
                case "CIT-Deployment":
                    if (entitiDetails.Buildwar == null)
                        entitiDetails.Buildwar = (string)stepContext.Result;
                    msg = $"Please confirm, Do you want to Deploy build for the war {entitiDetails.Buildwar} to {entitiDetails.Environment} environment ?";
                    return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = MessageFactory.Text(msg) }, cancellationToken);
                case "DB-Deployment":
                    if (entitiDetails.ScriptName.Equals("PM.SQL"))
                        entitiDetails.isDBRestricted = (bool)stepContext.Result;
                    msg = $"Please confirm, Do you want to proceed with {entitiDetails.ScriptName} deployment from {entitiDetails.Repo} repository with '{entitiDetails.Buildwar}' on {entitiDetails.DbInstance}?";
                    return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = MessageFactory.Text(msg) }, cancellationToken);
                case "RMI-Deployment":
                    if (string.IsNullOrEmpty(entitiDetails.Repo))
                        entitiDetails.Repo = (string)stepContext.Result;
                    msg = $"Please confirm, Do you want to proceed with RMI deployment on {entitiDetails.Environment} environment for {entitiDetails.Repo} repository?";
                    return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = MessageFactory.Text(msg) }, cancellationToken);
                default:
                    return await stepContext.NextAsync(entitiDetails, cancellationToken);
            }
        }
        private async Task<DialogTurnResult> CaptureEmailStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {  //var bookingDetails = await LuisHelper.ExecuteLuisQuery(Configuration, Logger, stepContext.Context, cancellationToken);
            var entitiDetails = (EntitiDetails)stepContext.Options;
            if (stepContext.Index > 9 || (bool)stepContext.Result)
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Enter your cotiviti email id to receive deployment status") }, cancellationToken);
            else
                return await stepContext.EndDialogAsync(null, cancellationToken);
        }
        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var entitiDetails = (EntitiDetails)stepContext.Options;
            entitiDetails.Email = (string)stepContext.Result;
            if (!(entitiDetails.Email.ToLower().Contains("@cotiviti.com")))
            {
                stepContext.ActiveDialog.State["stepIndex"] = (int)stepContext.ActiveDialog.State["stepIndex"] - 1;
                return await CaptureEmailStepAsync(stepContext, cancellationToken);
            }
            return await stepContext.EndDialogAsync(entitiDetails, cancellationToken);


        }
        private IList<Choice> GetProjectChoices(string sProject, string sRole)
        {
            switch (sProject.ToUpper())
            {

                case "BUILD_DEPLOYMENT":
                    var cardOptions = new List<Choice>()
            {
                new Choice() { Value = "App-Deployment", Synonyms = new List<string>() { "App" } },
                new Choice() { Value = "Informatica-Deployment", Synonyms = new List<string>() { "ETL" } },
                new Choice() { Value = "RMI-Deployment", Synonyms = new List<string>() { "RMI" } },
                new Choice() { Value = "CIT-Deployment", Synonyms = new List<string>() { "CIT","WIT" } },
                new Choice() { Value = "DB-Deployment",  Synonyms = new List<string>() { "DB" } }
            };
                    return cardOptions;
                case "DB-DEPLOYMENT":
                    MainDialog mainDialog = new MainDialog(Configuration, null, null);
                    string[] actions = mainDialog.getActions(sRole, "SubAction");
                    cardOptions = new List<Choice>();
                    foreach(string action in actions)
                    cardOptions.Add(new Choice() { Value = action, Synonyms = new List<string>() { action } });
                    //{
                    //new Choice() { Value = "PM.SQL", Synonyms = new List<string>() { "QA(VPMTST1)" } },
                    //new Choice() { Value = "API.SQL", Synonyms = new List<string>() { "SprintTest(VPMSPTE)" } },
                    //new Choice() { Value = "ICMSDB.SQL", Synonyms = new List<string>() { "Sprint Demo(VPMDEMO)" } },
                    //new Choice() { Value = "Others", Synonyms = new List<string>() { "CICD(VPMCICD)" } }, };
                    return cardOptions;
                default:
                    return null;
            }
        }

        private IList<Choice> GetEnvironments(string strType)
        {
            switch (strType)
            {
                case "App-Deployment":
                    var cardOptions = new List<Choice>() {
                    new Choice() { Value = "QA", Synonyms = new List<string>() { "QA" } },
                    new Choice() { Value = "SPTE", Synonyms = new List<string>() { "SPTE" } },
                    new Choice() { Value = "DEMO", Synonyms = new List<string>() { "DEMO" } },
                    new Choice() { Value = "CICD", Synonyms = new List<string>() { "CICD" } },
                    };
                    return cardOptions;
                case "Informatica-Deployment":
                    cardOptions = new List<Choice>() {
                    new Choice() { Value = "SBX", Synonyms = new List<string>() { "SBX" } },
                    new Choice() { Value = "SDEV", Synonyms = new List<string>() { "CICD" } },
                    new Choice() { Value = "SQA", Synonyms = new List<string>() { "SPTE" } },
                    new Choice() { Value = "SUAT", Synonyms = new List<string>() { "DEMO" } },
                    new Choice() { Value = "QA", Synonyms = new List<string>() { "QA" } },
                    };
                    return cardOptions;
                case "RMI-Deployment":
                case "DB-Deployment":
                    cardOptions = new List<Choice>() {
                    new Choice() { Value = "QA(VPMTST1)", Synonyms = new List<string>() { "QA(VPMTST1)" } },
                    new Choice() { Value = "SprintTest(VPMSPTE)", Synonyms = new List<string>() { "SprintTest(VPMSPTE)" } },
                    new Choice() { Value = "SprintDemo(VPMDEMO)", Synonyms = new List<string>() { "Sprint Demo(VPMDEMO)" } },
                    new Choice() { Value = "CICD(VPMCICD)", Synonyms = new List<string>() { "CICD(VPMCICD)" } }, };
                    return cardOptions;
                case "CIT-Deployment":
                    cardOptions = new List<Choice>() {
                    new Choice() { Value = "DEV", Synonyms = new List<string>() { "DEV" } },
                    new Choice() { Value = "QA", Synonyms = new List<string>() { "QA" } },};
                    return cardOptions;
                default:
                    return null;
            }
        }

        private HttpClient getHttpClient(string url, bool isAuthreq)
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri(url);
            if (isAuthreq)
                client.DefaultRequestHeaders.Add("Authorization", Configuration["BitbuketAuthKey"]);
            return client;

        }
        private IList<Choice> GetBranches()
        {

            var url = Configuration["ETLBitbucketUrl"];
            var client = getHttpClient(Configuration["ETLBitbucketUrl"], true);
            HttpResponseMessage response = client.GetAsync(url).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
            var res = (JObject)JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
            var values = res["values"].Children();
            var cardOptions = new List<Choice>();
            cardOptions.Add(new Choice() { Value = "Skip" });
            foreach (var value in values)
                cardOptions.Add(new Choice() { Value = value["displayId"].ToString() });
            return cardOptions;

        }


        private IList<Choice> GetNexusVersions()
        {
            try
            {
                var xmlFile = Configuration["ETLNexusURL"];
                var client = getHttpClient(Configuration["ETLNexusURL"], false);
                HttpResponseMessage response = client.GetAsync(xmlFile).GetAwaiter().GetResult();
                XDocument xmlDoc = XDocument.Parse(response.Content.ReadAsStringAsync().Result);
                var xmlNodes = xmlDoc.Descendants("version");
                var cardOptions = new List<Choice>();
                cardOptions.Add(new Choice() { Value = "Skip" });
                foreach (var item in xmlNodes)
                    cardOptions.Add(new Choice() { Value = item.Value });
                return cardOptions;
            }
            catch (Exception ex)
            {

                throw;
            }

        }

    }
}
