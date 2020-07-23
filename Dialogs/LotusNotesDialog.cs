using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.BotBuilderSamples;
using Microsoft.BotBuilderSamples.Dialogs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoreBot.Dialogs
{
    public class LotusNotesDialog : CancelAndHelpDialog
    {
        protected readonly IConfiguration Configuration;
        protected readonly ILogger Logger;
        public LotusNotesDialog(IConfiguration configuration, ILogger<MainDialog> logger)
            : base(nameof(LotusNotesDialog))
        {
            Configuration = configuration;
            Logger = logger;
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new DateResolverDialog());
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {

                RuleactionstepAsync,
                UserStepAsync,
                MidruleStepAsync,
                ConfirmStepAsync,
                CaptureEmailStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> RuleactionstepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            var entitiDetails = (EntitiDetails)stepContext.Options;
            if (entitiDetails.Intent.Equals("RuleRelationShip"))
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please enter Rule") }, cancellationToken);
            return await stepContext.PromptAsync(nameof(ChoicePrompt),
                     new PromptOptions
                     {
                         Prompt = MessageFactory.Text("Please select option below"),
                         Choices = GetChoices("Actions"),
                         RetryPrompt = MessageFactory.Text("Sorry, I'm still learning. Please provide the valid option or below mentioned Sequence Number."),
                     }, cancellationToken);


        }
        private async Task<DialogTurnResult> UserStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            var entitiDetails = (EntitiDetails)stepContext.Options;
            ManageRule manageRule = new ManageRule();
            if (entitiDetails.Intent.Equals("RuleRelationShip"))
            {
                manageRule.Description = (string)stepContext.Result;
                entitiDetails.manageRule = manageRule;
                return await stepContext.EndDialogAsync(entitiDetails, cancellationToken);

            }

            manageRule.Action = ((FoundChoice)stepContext.Result).Value.ToString();
            entitiDetails.Project = manageRule.Action;
            entitiDetails.manageRule = manageRule;
            if (manageRule.Action.Equals("PR Status"))
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please enter PR Id") }, cancellationToken);
            else if (manageRule.Action.Equals("Deactivate Rule"))
                return await stepContext.NextAsync(entitiDetails, cancellationToken);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please enter lotus notes user") }, cancellationToken);
        }

        private async Task<DialogTurnResult> MidruleStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var entitiDetails = (EntitiDetails)stepContext.Options;
            entitiDetails.Project = "LotusNotes";
            if (entitiDetails.manageRule.Action.Equals("PR Creation"))
            {
                entitiDetails.manageRule.LotusNotesUser = (string)stepContext.Result;
                return await stepContext.NextAsync(entitiDetails, cancellationToken);
            }
            else if (entitiDetails.manageRule.Action.Equals("PR Status"))
            {
                entitiDetails.manageRule.PresentationId = (string)stepContext.Result;
                return await stepContext.NextAsync(entitiDetails, cancellationToken);
            }
            else if (entitiDetails.manageRule.Action.Equals("Deactivate Rule"))
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please enter Mid Rule version") }, cancellationToken);
            else
            {
                entitiDetails.manageRule.LotusNotesUser = (string)stepContext.Result;
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please enter Mid Rule version") }, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var entitiDetails = (EntitiDetails)stepContext.Options;
            if (entitiDetails.manageRule.Action.Equals("PR Creation"))
                entitiDetails.Tag = "CreatePRMID";
            else if (entitiDetails.manageRule.Action.Equals("PR Status"))
                entitiDetails.Tag = "GetPRStatus";
            else if (entitiDetails.manageRule.Action.Equals("Deactivate Rule"))
            {
                entitiDetails.manageRule.MidruleVersion = (string)stepContext.Result;
                entitiDetails.Tag = "DeactivateRule";
            }
            else
            {
                entitiDetails.manageRule.MidruleVersion = (string)stepContext.Result;
                entitiDetails.Tag = "CreateLogicalRMRID";
            }
            var msg = $"Please confirm, Do you want to  {entitiDetails.manageRule.Action.Replace("RMR and changeLog generation", " create RMR and generate changeLog")} {" "}  ?";
            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = MessageFactory.Text(msg) }, cancellationToken);
        }
        private async Task<DialogTurnResult> CaptureEmailStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var entitiDetails = (EntitiDetails)stepContext.Options;
            if (stepContext.Result == null || (bool)stepContext.Result)
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Enter your cotiviti email id to receive the PR Id details") }, cancellationToken);
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
        private IList<Choice> GetChoices(string strPortFolio)
        {
            switch (strPortFolio.ToUpper())
            {
                case "ACTIONS":
                    var cardOptions = new List<Choice>()
            {
                new Choice() { Value = "PR Creation", Synonyms = new List<string>() { "Create PR" } },
                new Choice() { Value = "PR Status", Synonyms = new List<string>() { "PR status" } },
                new Choice() { Value = "Deactivate Rule", Synonyms = new List<string>() { "Deactivate" } },
                new Choice() { Value = "RMR and changeLog generation", Synonyms = new List<string>() { "RMR" } },
            };
                    return cardOptions;
                default:
                    return null;
            }
        }
    }
}
