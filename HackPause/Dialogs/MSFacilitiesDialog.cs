using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.BotBuilderSamples.Dialogs;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoreBot.Dialogs
{
    public class MSFacilitiesDialog : CancelAndHelpDialog
    {
        public MSFacilitiesDialog()
            : base(nameof(MSFacilitiesDialog))
        {
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new DateResolverDialog());
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                GetIssueAsync,
                GetLocationAsync,
                ConfirmStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> GetIssueAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var ticketDetails = (MSFacilitiesTicket)stepContext.Options;

            if (ticketDetails.IssueName == null)
            {
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("What problem are you facing?") }, cancellationToken);
            }
            else
            {
                return await stepContext.NextAsync(ticketDetails.IssueName, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> GetLocationAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var ticketDetails = (MSFacilitiesTicket)stepContext.Options;

            ticketDetails.IssueName = (string)stepContext.Result;

            if (ticketDetails.Location == null)
            {
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Sure, I get it. Where are you facing the issue (Which Workstation/Room)?") }, cancellationToken);
            }
            else
            {
                return await stepContext.NextAsync(ticketDetails.Location, cancellationToken);
            }
        }
        /*private async Task<DialogTurnResult> TravelDateStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var bookingDetails = (ticketDetails)stepContext.Options;

            bookingDetails.Origin = (string)stepContext.Result;

            if (bookingDetails.TravelDate == null || IsAmbiguous(bookingDetails.TravelDate))
            {
                return await stepContext.BeginDialogAsync(nameof(DateResolverDialog), bookingDetails.TravelDate, cancellationToken);
            }
            else
            {
                return await stepContext.NextAsync(bookingDetails.TravelDate, cancellationToken);
            }
        }*/

        private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var ticketDetails = (MSFacilitiesTicket)stepContext.Options;

            ticketDetails.Location = (string)stepContext.Result;

            var msg = $"Please confirm, You are facing the issue : {ticketDetails.IssueName} for: {ticketDetails.Location}";

            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = MessageFactory.Text(msg) }, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result)
            {
                var ticketDetails = (MSFacilitiesTicket)stepContext.Options;

                return await stepContext.EndDialogAsync(ticketDetails, cancellationToken);
            }
            else
            {
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
        }

        private static bool IsAmbiguous(string timex)
        {
            var timexProperty = new TimexProperty(timex);
            return !timexProperty.Types.Contains(Constants.TimexTypes.Definite);
        }
    }
}
