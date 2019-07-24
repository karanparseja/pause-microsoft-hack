using Microsoft.Bot.Builder.Dialogs;
using Microsoft.BotBuilderSamples.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreBot
{
    public class DoctorAppointment
    {
        public string symptom { get; set; }

        public string timeslot { get; set; }

        public string doctorName { get; set; }

    }
}
