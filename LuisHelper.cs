// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Json;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Microsoft.BotBuilderSamples
{
    public static class LuisHelper
    {
        public static async Task<EntitiDetails> ExecuteLuisQuery(IConfiguration configuration, ILogger logger, ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var entitiDetails = new EntitiDetails();

            try
            {
                // Create the LUIS settings from configuration.
                var luisApplication = new LuisApplication(
                    configuration["LuisAppId"],
                    configuration["LuisAPIKey"],
                    "https://" + configuration["LuisAPIHostName"]
                );

                var recognizer = new LuisRecognizer(luisApplication);

                // The actual call to LUIS
                var recognizerResult = await recognizer.RecognizeAsync(turnContext, cancellationToken);

                var (intent, score) = recognizerResult.GetTopScoringIntent();

                entitiDetails.Intent = intent;
                entitiDetails.Score = score;
                //if(score<0.3)
                //    return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("which Acronyn details would you like to have") }, cancellationToken)
                if (intent == "Trigger_Service" || intent == "Build_Deployment")
                {

                    // We need to get the result from the LUIS JSON which at every level returns an array.
                    //entitiDetails.Project = recognizerResult.Entities["service"]?.FirstOrDefault()?["Tag"]?.FirstOrDefault()?.FirstOrDefault()?.ToString();
                    if (recognizerResult.Entities["service"] != null)
                        entitiDetails.Project = recognizerResult.Entities["service"].First().ToString().Replace("[\r\n  \"", string.Empty).Replace("\"\r\n]", string.Empty);
                    if (recognizerResult.Entities["Tag"] != null)
                        entitiDetails.Tag = recognizerResult.Entities["Tag"].First().ToString().Replace("[\r\n  \"", string.Empty).Replace("\"\r\n]", string.Empty);
                    if (recognizerResult.Entities["Var"] != null)
                        entitiDetails.Buildwar = recognizerResult.Entities["Var"].First().ToString().Replace("[\r\n  \"", string.Empty).Replace("\"\r\n]", string.Empty);
                    if (recognizerResult.Entities["Portfolio"] != null)
                        entitiDetails.Portfolio = recognizerResult.Entities["Portfolio"].First().ToString().Replace("[\r\n  \"", string.Empty).Replace("\"\r\n]", string.Empty);
                    if (recognizerResult.Entities["Environment"] != null)
                        entitiDetails.Environment = recognizerResult.Entities["Environment"].First().ToString().Replace("[\r\n  \"", string.Empty).Replace("\"\r\n]", string.Empty);
                    entitiDetails.TravelDate = recognizerResult.Entities["datetime"]?.FirstOrDefault()?["timex"]?.FirstOrDefault()?.ToString();
                }
                else if (intent == "Acronym")
                {
                    entitiDetails.Acronym = recognizerResult.Entities["Word"].First().ToString().Replace("[\r\n  \"", string.Empty).Replace("\"\r\n]", string.Empty);                   
                }
            }
            catch (Exception e)
            {
                logger.LogWarning($"LUIS Exception: {e.Message} Check your LUIS configuration.");
            }

            return entitiDetails;
        }
    }

}
