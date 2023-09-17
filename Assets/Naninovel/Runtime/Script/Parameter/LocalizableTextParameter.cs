// Copyright 2023 ReWaffle LLC. All rights reserved.

using System;

namespace Naninovel
{
    /// <summary>
    /// Represents a serializable <see cref="Command"/> parameter with <see cref="LocalizableText"/> value.
    /// </summary>
    [Serializable]
    public class LocalizableTextParameter : CommandParameter<LocalizableText>
    {
        public static implicit operator LocalizableTextParameter (string value) => new LocalizableTextParameter { Value = value };
        public static implicit operator LocalizableTextParameter (LocalizableText value) => new LocalizableTextParameter { Value = value };
        public static implicit operator LocalizableText? (LocalizableTextParameter param) => !Command.Assigned(param) ? null : (LocalizableText?)param.Value;
        public static implicit operator string (LocalizableTextParameter value) => value.Value;

        protected override LocalizableText ParseRaw (RawValue raw, out string errors)
        {
            errors = null;

            var parts = new LocalizableTextPart[raw.Parts.Count];

            for (int i = 0; i < raw.Parts.Count; i++)
                if (raw.Parts[i].Kind == ParameterValuePartKind.IdentifiedText)
                    parts[i] = LocalizableTextPart.FromId(raw.Parts[i].Id, PlaybackSpot?.ScriptName ?? (errors = "Unassigned playback spot."));
                else if (raw.Parts[i].Kind == ParameterValuePartKind.PlainText)
                    parts[i] = LocalizableTextPart.FromPlainText(raw.Parts[i].Text);
                else errors = "Unexpected parameter value part. Expressions should be handled by a master parsing routine.";

            return new LocalizableText(parts);
        }
    }
}
