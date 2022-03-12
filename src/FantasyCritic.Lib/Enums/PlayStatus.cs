using System;
using System.Collections.Generic;
using System.Text;

namespace FantasyCritic.Lib.Enums
{
    public class PlayStatus : TypeSafeEnum<PlayStatus>
    {
        // Define values here.
        public static readonly PlayStatus NotStartedDraft = new PlayStatus("NotStartedDraft");
        public static readonly PlayStatus Drafting = new PlayStatus("Drafting");
        public static readonly PlayStatus DraftPaused = new PlayStatus("DraftPaused");
        public static readonly PlayStatus DraftFinal = new PlayStatus("DraftFinal");

        // Constructor is private: values are defined within this class only!
        private PlayStatus(string value)
            : base(value)
        {

        }

        public bool PlayStarted => (Value != NotStartedDraft.Value);
        public bool DraftFinished => (Value == DraftFinal.Value);
        public bool DraftIsActive => (Value == Drafting.Value);
        public bool DraftIsPaused => (Value == DraftPaused.Value);

        public override string ToString() => Value;
    }
}
