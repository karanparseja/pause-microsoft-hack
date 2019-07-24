// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoreBot;
using CoreBot.Dialogs;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;

namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        protected readonly IConfiguration Configuration;
        protected readonly ILogger Logger;
        public string detectedIntent;
        public MainDialog(IConfiguration configuration, ILogger<MainDialog> logger)
            : base(nameof(MainDialog))
        {
            Configuration = configuration;
            Logger = logger;

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new MSFacilitiesDialog());
            AddDialog(new DoctorDialog());
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                IntroStepAsync,
                ActStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(Configuration["LuisAppId"]) || string.IsNullOrEmpty(Configuration["LuisAPIKey"]) || string.IsNullOrEmpty(Configuration["LuisAPIHostName"]))
            {
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text("NOTE: LUIS is not configured. To enable all capabilities, add 'LuisAppId', 'LuisAPIKey' and 'LuisAPIHostName' to the appsettings.json file."), cancellationToken);

                return await stepContext.NextAsync(null, cancellationToken);
            }
            else
            {
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("What can I help you with today?\nSay something like \"It's too hot at my workstation\" or \"I'm not feeling well today\"") }, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Call LUIS and gather any potential booking details. (Note the TurnContext has the response to the prompt.)
            var luisResult = stepContext.Result != null
                    ?
                await LuisHelper.ExecuteLuisQuery(Configuration, Logger, stepContext.Context, cancellationToken)
                    :
                new RecognizerResult();
            detectedIntent = luisResult.GetTopScoringIntent().intent;
            if (detectedIntent == "Open_MS_Facilities_Ticket") {
                MSFacilitiesTicket ticketDetails = new MSFacilitiesTicket();
                ticketDetails.IssueName = luisResult.Entities["IssueName"]?.FirstOrDefault()?["text"]?.ToString();
                ticketDetails.Location = luisResult.Entities["Meeting_Room"]?.First()?.ToString();
                return await stepContext.BeginDialogAsync(nameof(MSFacilitiesDialog), ticketDetails, cancellationToken);
            }
            if (detectedIntent == "Book_Doctor_Appointment")
            {
                DoctorAppointment doctorAppointment = new DoctorAppointment();
                doctorAppointment.symptom = luisResult.Entities["Symptom"]?.FirstOrDefault()?.FirstOrDefault()?.ToString();
                //doctorAppointment.timeslot = luisResult.Entities[""]?.First()?.ToString();
                return await stepContext.BeginDialogAsync(nameof(DoctorDialog), doctorAppointment, cancellationToken);
            }
            // In this sample we only have a single Intent we are concerned with. However, typically a scenario
            // will have multiple different Intents each corresponding to starting a different child Dialog.

            // Run the BookingDialog giving it whatever details we have from the LUIS call, it will fill out the remainder.
            return await stepContext.BeginDialogAsync(nameof(BookingDialog), luisResult, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // If the child dialog ("BookingDialog") was cancelled or the user failed to confirm, the Result here will be null.
            if (stepContext.Result != null)
            {
                var msg = $"";
                if (detectedIntent == "Open_MS_Facilities_Ticket")
                {
                    var result = (MSFacilitiesTicket)stepContext.Result;
                    DateTime now = DateTime.Now;
                    Random generator = new Random();
                    String r = generator.Next(0, 999999).ToString("D6");
                    msg = $"I have assigned MS Facilities a ticket for {result.IssueName} at {result.Location} on {now}.\nThe Ticket number for reference is {r}";
                }
                if (detectedIntent == "Book_Doctor_Appointment")
                {
                    var result = (DoctorAppointment)stepContext.Result;
                    DateTime now = DateTime.Now;
                    Random generator = new Random();
                    String r = generator.Next(0, 999999).ToString("D4");
                    msg = $"I have made a Doctor Appointment for {result.symptom} at {result.timeslot} {result.doctorName} on {now}.\nThe Appointment Number is {r}";
                }
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(msg), cancellationToken);
                /*var result = (BookingDetails)stepContext.Result;

                // Now we have all the booking details call the booking service.

                // If the call to the booking service was successful tell the user.

                var timeProperty = new TimexProperty(result.TravelDate);
                var travelDateMsg = timeProperty.ToNaturalLanguage(DateTime.Now);
                var msg = $"I have you booked to {result.Destination} from {result.Origin} on {travelDateMsg}";
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(msg), cancellationToken);
                */
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Thank you."), cancellationToken);
            }
            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }
    }
}
