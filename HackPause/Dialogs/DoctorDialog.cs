using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.BotBuilderSamples.Dialogs;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoreBot.Dialogs
{
    public class DoctorDialog : CancelAndHelpDialog
    {
        public DoctorDialog()
            : base(nameof(DoctorDialog))
        {
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new DateResolverDialog());
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                GetSymptomAsync,
                GetTimeSlotAsync,
                ConfirmStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> GetSymptomAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var doctorAppointment = (DoctorAppointment)stepContext.Options;

            if (doctorAppointment.symptom == null)
            {
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Don't worry I can help you out. Could you describe some symptoms that you are facing ?") }, cancellationToken);
            }
            else
            {
                return await stepContext.NextAsync(doctorAppointment.symptom, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> GetTimeSlotAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var doctorAppointment = (DoctorAppointment)stepContext.Options;

            doctorAppointment.symptom = (string)stepContext.Result;

            if (doctorAppointment.timeslot == null)
            {
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Let me Book a Doctor Appointment for you. Suitable time for the booking?") }, cancellationToken);
            }
            else
            {
                return await stepContext.NextAsync(doctorAppointment.timeslot, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var doctorAppointment = (DoctorAppointment)stepContext.Options;

            doctorAppointment.timeslot = (string)stepContext.Result;

            IDictionary<string, string> symptomDoctorMap = new Dictionary<string, string>();
            symptomDoctorMap.Add("fever", "Dr. Paracitimol");
            symptomDoctorMap.Add("bodypain", "Dr. Moov");
            symptomDoctorMap.Add("fracture", "Dr. Strange");

            var closestKey = doctorAppointment.symptom.Trim().Replace(" ", "").ToLower();
            closestKey = symptomDoctorMap.Keys.ToList().Select(x => closestKey.Contains(x) ? x : string.Empty).FirstOrDefault();
            var isSymptomMapped = symptomDoctorMap.TryGetValue(closestKey, out string mappedDoctor);
            string msg = String.Empty;
            doctorAppointment.doctorName = mappedDoctor;

            if (isSymptomMapped) msg = $"I am booking an appointment : {doctorAppointment.symptom} at {mappedDoctor} for: {doctorAppointment.timeslot}";
            else msg = $"I am booking an appointment : {doctorAppointment.symptom} for: {doctorAppointment.timeslot}";

            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = MessageFactory.Text(msg) }, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result)
            {
                var doctorAppointment = (DoctorAppointment)stepContext.Options;

                return await stepContext.EndDialogAsync(doctorAppointment, cancellationToken);
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
